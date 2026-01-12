using Ledger.Domain.Events;
using Platform.BuildingBlocks.Domain;
using Platform.BuildingBlocks.HashChain;

namespace Ledger.Domain.Entities;

/// <summary>
/// Ledger Entry - IMMUTABLE financial record.
/// Implements hash chain for tamper detection.
/// 
/// CRITICAL SECURITY RULES:
/// - Entries are NEVER updated or deleted
/// - Corrections are made via ReversalEntry
/// - Each entry contains hash of previous entry
/// </summary>
public class LedgerEntry : Entity, IHashableRecord
{
    private readonly List<PostingLine> _postingLines = new();

    private LedgerEntry() { }

    /// <summary>
    /// Sequential number in the ledger chain.
    /// </summary>
    public long SequenceNumber { get; private set; }

    /// <summary>
    /// Reference to the originating transaction (e.g., PaymentIntent ID).
    /// </summary>
    public Guid ReferenceId { get; private set; }

    /// <summary>
    /// Type of reference (Payment, Refund, Adjustment, etc.)
    /// </summary>
    public string ReferenceType { get; private set; } = null!;

    /// <summary>
    /// Human-readable reference number.
    /// </summary>
    public string ReferenceNumber { get; private set; } = null!;

    /// <summary>
    /// Description of the entry.
    /// </summary>
    public string Description { get; private set; } = null!;

    /// <summary>
    /// Entry type (CREDIT or DEBIT).
    /// </summary>
    public LedgerEntryType EntryType { get; private set; }

    /// <summary>
    /// Total amount (must equal sum of posting lines).
    /// </summary>
    public decimal TotalAmount { get; private set; }

    /// <summary>
    /// Currency code.
    /// </summary>
    public string Currency { get; private set; } = null!;

    /// <summary>
    /// When the entry was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// User who created the entry.
    /// </summary>
    public Guid CreatedByUserId { get; private set; }

    /// <summary>
    /// Hash of the previous entry in the chain.
    /// </summary>
    public string PreviousHash { get; private set; } = null!;

    /// <summary>
    /// Hash of this entry.
    /// Hn = SHA-256(CanonicalJSON(Entry) + Hn-1)
    /// </summary>
    public string Hash { get; private set; } = null!;

    /// <summary>
    /// If this is a reversal, points to the original entry.
    /// </summary>
    public Guid? ReversedEntryId { get; private set; }

    /// <summary>
    /// If this entry has been reversed, points to the reversal.
    /// </summary>
    public Guid? ReversalEntryId { get; private set; }

    /// <summary>
    /// Posting lines that make up this entry.
    /// </summary>
    public IReadOnlyList<PostingLine> PostingLines => _postingLines.AsReadOnly();

    /// <summary>
    /// Creates a new ledger entry.
    /// </summary>
    public static LedgerEntry Create(
        long sequenceNumber,
        Guid referenceId,
        string referenceType,
        string referenceNumber,
        string description,
        LedgerEntryType entryType,
        decimal totalAmount,
        string currency,
        Guid createdByUserId,
        string previousHash,
        IHashChainService hashChainService,
        IEnumerable<PostingLine>? postingLines = null)
    {
        var entry = new LedgerEntry
        {
            Id = Guid.NewGuid(),
            SequenceNumber = sequenceNumber,
            ReferenceId = referenceId,
            ReferenceType = referenceType,
            ReferenceNumber = referenceNumber,
            Description = description,
            EntryType = entryType,
            TotalAmount = totalAmount,
            Currency = currency.ToUpperInvariant(),
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = createdByUserId,
            PreviousHash = previousHash
        };

        if (postingLines != null)
        {
            entry._postingLines.AddRange(postingLines);
        }

        // Compute hash
        entry.Hash = hashChainService.ComputeHash(entry.GetHashableData(), previousHash);

        entry.RaiseDomainEvent(new LedgerEntryCreatedEvent(
            entry.Id,
            entry.SequenceNumber,
            entry.ReferenceId,
            entry.TotalAmount,
            entry.Currency));

        return entry;
    }

    /// <summary>
    /// Creates a reversal entry for this entry.
    /// </summary>
    public static LedgerEntry CreateReversal(
        LedgerEntry originalEntry,
        long sequenceNumber,
        string reason,
        Guid createdByUserId,
        string previousHash,
        IHashChainService hashChainService)
    {
        if (originalEntry.ReversalEntryId.HasValue)
        {
            throw new InvalidOperationException("Entry has already been reversed");
        }

        var reversalEntry = new LedgerEntry
        {
            Id = Guid.NewGuid(),
            SequenceNumber = sequenceNumber,
            ReferenceId = originalEntry.ReferenceId,
            ReferenceType = "Reversal",
            ReferenceNumber = $"REV-{originalEntry.ReferenceNumber}",
            Description = $"Reversal: {reason}",
            EntryType = originalEntry.EntryType == LedgerEntryType.Credit 
                ? LedgerEntryType.Debit 
                : LedgerEntryType.Credit,
            TotalAmount = originalEntry.TotalAmount,
            Currency = originalEntry.Currency,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = createdByUserId,
            PreviousHash = previousHash,
            ReversedEntryId = originalEntry.Id
        };

        // Reverse posting lines
        foreach (var line in originalEntry.PostingLines)
        {
            reversalEntry._postingLines.Add(PostingLine.Create(
                line.AccountCode,
                line.AccountName,
                line.Amount,
                line.PostingType == PostingType.Debit ? PostingType.Credit : PostingType.Debit));
        }

        // Compute hash
        reversalEntry.Hash = hashChainService.ComputeHash(reversalEntry.GetHashableData(), previousHash);

        reversalEntry.RaiseDomainEvent(new LedgerReversalCreatedEvent(
            reversalEntry.Id,
            originalEntry.Id,
            reversalEntry.TotalAmount,
            reason));

        return reversalEntry;
    }

    /// <summary>
    /// Marks this entry as reversed (called on the original entry).
    /// </summary>
    internal void MarkAsReversed(Guid reversalEntryId)
    {
        ReversalEntryId = reversalEntryId;
    }

    /// <summary>
    /// Gets the data to be hashed (excluding hash fields).
    /// </summary>
    public object GetHashableData()
    {
        return new
        {
            Id,
            SequenceNumber,
            ReferenceId,
            ReferenceType,
            ReferenceNumber,
            Description,
            EntryType = EntryType.ToString(),
            TotalAmount,
            Currency,
            CreatedAt = CreatedAt.ToString("O"),
            CreatedByUserId,
            ReversedEntryId,
            PostingLines = _postingLines.Select(p => new
            {
                p.AccountCode,
                p.AccountName,
                p.Amount,
                PostingType = p.PostingType.ToString()
            }).ToList()
        };
    }
}

/// <summary>
/// Type of ledger entry.
/// </summary>
public enum LedgerEntryType
{
    Credit = 1,
    Debit = 2
}

/// <summary>
/// Individual posting line within a ledger entry.
/// </summary>
public class PostingLine : ValueObject
{
    private PostingLine() { }

    public string AccountCode { get; private set; } = null!;
    public string AccountName { get; private set; } = null!;
    public decimal Amount { get; private set; }
    public PostingType PostingType { get; private set; }

    public static PostingLine Create(
        string accountCode,
        string accountName,
        decimal amount,
        PostingType postingType)
    {
        return new PostingLine
        {
            AccountCode = accountCode,
            AccountName = accountName,
            Amount = amount,
            PostingType = postingType
        };
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return AccountCode;
        yield return Amount;
        yield return PostingType;
    }
}

/// <summary>
/// Type of posting (debit or credit side).
/// </summary>
public enum PostingType
{
    Debit = 1,
    Credit = 2
}
