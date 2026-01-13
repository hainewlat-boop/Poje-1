using Platform.BuildingBlocks.Domain;
using Platform.BuildingBlocks.HashChain;

namespace Audit.Domain.Entities;

/// <summary>
/// Audit Event - IMMUTABLE, APPEND-ONLY record.
/// Implements hash chain for tamper detection.
/// 
/// CRITICAL SECURITY RULES:
/// - Events are NEVER updated or deleted
/// - Each event contains hash of previous event
/// - Hash chain is verified periodically
/// - Chain break triggers CRITICAL alert and read-only mode
/// </summary>
public class AuditEvent : Entity, IHashableRecord
{
    private AuditEvent() { }

    /// <summary>
    /// Sequential number in the audit chain.
    /// </summary>
    public long SequenceNumber { get; private set; }

    /// <summary>
    /// Type of audit event (e.g., auth.login.success).
    /// </summary>
    public string EventType { get; private set; } = null!;

    /// <summary>
    /// Action performed (create, read, update, delete, etc.).
    /// </summary>
    public string Action { get; private set; } = null!;

    /// <summary>
    /// Resource type (user, application, payment, etc.).
    /// </summary>
    public string Resource { get; private set; } = null!;

    /// <summary>
    /// Resource ID if applicable.
    /// </summary>
    public Guid? ResourceId { get; private set; }

    /// <summary>
    /// User who performed the action.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// User email for quick reference.
    /// </summary>
    public string? UserEmail { get; private set; }

    /// <summary>
    /// IP address of the request.
    /// </summary>
    public string IpAddress { get; private set; } = null!;

    /// <summary>
    /// User agent of the request.
    /// </summary>
    public string? UserAgent { get; private set; }

    /// <summary>
    /// Session ID.
    /// </summary>
    public string? SessionId { get; private set; }

    /// <summary>
    /// Correlation ID for distributed tracing.
    /// </summary>
    public string? CorrelationId { get; private set; }

    /// <summary>
    /// When the event occurred.
    /// </summary>
    public DateTime Timestamp { get; private set; }

    /// <summary>
    /// Whether the action was successful.
    /// </summary>
    public bool IsSuccess { get; private set; }

    /// <summary>
    /// Error message if the action failed.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Additional details as JSON.
    /// </summary>
    public string? DetailsJson { get; private set; }

    /// <summary>
    /// Hash of the previous event in the chain.
    /// </summary>
    public string PreviousHash { get; private set; } = null!;

    /// <summary>
    /// Hash of this event.
    /// Hn = SHA-256(CanonicalJSON(Event) + Hn-1)
    /// </summary>
    public string Hash { get; private set; } = null!;

    /// <summary>
    /// Creates a new audit event.
    /// </summary>
    public static AuditEvent Create(
        long sequenceNumber,
        string eventType,
        string action,
        string resource,
        Guid userId,
        string ipAddress,
        string previousHash,
        IHashChainService hashChainService,
        Guid? resourceId = null,
        string? userEmail = null,
        string? userAgent = null,
        string? sessionId = null,
        string? correlationId = null,
        bool isSuccess = true,
        string? errorMessage = null,
        string? detailsJson = null)
    {
        var auditEvent = new AuditEvent
        {
            Id = Guid.NewGuid(),
            SequenceNumber = sequenceNumber,
            EventType = eventType,
            Action = action,
            Resource = resource,
            ResourceId = resourceId,
            UserId = userId,
            UserEmail = userEmail,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            SessionId = sessionId,
            CorrelationId = correlationId,
            Timestamp = DateTime.UtcNow,
            IsSuccess = isSuccess,
            ErrorMessage = errorMessage,
            DetailsJson = detailsJson,
            PreviousHash = previousHash
        };

        // Compute hash
        auditEvent.Hash = hashChainService.ComputeHash(auditEvent.GetHashableData(), previousHash);

        return auditEvent;
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
            EventType,
            Action,
            Resource,
            ResourceId,
            UserId,
            UserEmail,
            IpAddress,
            UserAgent,
            SessionId,
            CorrelationId,
            Timestamp = Timestamp.ToString("O"),
            IsSuccess,
            ErrorMessage,
            DetailsJson
        };
    }
}

/// <summary>
/// Stores the current state of the hash chain.
/// </summary>
public class HashChainState : Entity
{
    private HashChainState() { }

    /// <summary>
    /// Chain identifier (audit, ledger, etc.).
    /// </summary>
    public string ChainId { get; private set; } = null!;

    /// <summary>
    /// Genesis hash for the chain.
    /// </summary>
    public string GenesisHash { get; private set; } = null!;

    /// <summary>
    /// Last sequence number in the chain.
    /// </summary>
    public long LastSequenceNumber { get; private set; }

    /// <summary>
    /// Hash of the last entry.
    /// </summary>
    public string LastHash { get; private set; } = null!;

    /// <summary>
    /// When the chain was last verified.
    /// </summary>
    public DateTime LastVerifiedAt { get; private set; }

    /// <summary>
    /// Whether the chain is currently valid.
    /// </summary>
    public bool IsValid { get; private set; }

    /// <summary>
    /// If broken, the sequence where it broke.
    /// </summary>
    public long? BrokenAtSequence { get; private set; }

    /// <summary>
    /// When the chain was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    public static HashChainState Create(string chainId, string genesisHash)
    {
        return new HashChainState
        {
            Id = Guid.NewGuid(),
            ChainId = chainId,
            GenesisHash = genesisHash,
            LastSequenceNumber = 0,
            LastHash = genesisHash,
            LastVerifiedAt = DateTime.UtcNow,
            IsValid = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateLastEntry(long sequenceNumber, string hash)
    {
        LastSequenceNumber = sequenceNumber;
        LastHash = hash;
    }

    public void MarkAsVerified()
    {
        LastVerifiedAt = DateTime.UtcNow;
        IsValid = true;
    }

    public void MarkAsBroken(long brokenAtSequence)
    {
        IsValid = false;
        BrokenAtSequence = brokenAtSequence;
    }
}
