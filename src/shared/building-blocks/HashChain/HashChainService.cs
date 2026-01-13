using System.Security.Cryptography;
using System.Text;

namespace Platform.BuildingBlocks.HashChain;

/// <summary>
/// Service for computing and verifying hash chains.
/// Implements the formula: Hn = SHA-256(CanonicalJSON(Record) + Hn-1)
/// </summary>
public interface IHashChainService
{
    /// <summary>
    /// Computes the hash for a new record in the chain.
    /// </summary>
    string ComputeHash<T>(T record, string previousHash) where T : class;

    /// <summary>
    /// Verifies a hash value against the record and previous hash.
    /// </summary>
    bool VerifyHash<T>(T record, string previousHash, string expectedHash) where T : class;

    /// <summary>
    /// Generates the genesis hash for a new chain.
    /// </summary>
    string GenerateGenesisHash(string environmentId, string version);
}

/// <summary>
/// Implementation of hash chain service using SHA-256.
/// </summary>
public class HashChainService : IHashChainService
{
    /// <summary>
    /// Computes the hash for a new record in the chain.
    /// Hn = SHA-256(CanonicalJSON(Record) + Hn-1)
    /// </summary>
    public string ComputeHash<T>(T record, string previousHash) where T : class
    {
        ArgumentNullException.ThrowIfNull(record);
        ArgumentNullException.ThrowIfNull(previousHash);

        var canonicalJson = CanonicalJsonSerializer.Serialize(record);
        var input = canonicalJson + previousHash;
        
        return ComputeSha256(input);
    }

    /// <summary>
    /// Verifies a hash value against the record and previous hash.
    /// </summary>
    public bool VerifyHash<T>(T record, string previousHash, string expectedHash) where T : class
    {
        var computedHash = ComputeHash(record, previousHash);
        return string.Equals(computedHash, expectedHash, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Generates the genesis hash for a new chain.
    /// Genesis = SHA-256(ENV_ID + VERSION + TIMESTAMP)
    /// </summary>
    public string GenerateGenesisHash(string environmentId, string version)
    {
        ArgumentNullException.ThrowIfNull(environmentId);
        ArgumentNullException.ThrowIfNull(version);

        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        var input = $"{environmentId}|{version}|{timestamp}";
        
        return ComputeSha256(input);
    }

    private static string ComputeSha256(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}

/// <summary>
/// Result of hash chain verification.
/// </summary>
public record HashChainVerificationResult
{
    public bool IsValid { get; init; }
    public int TotalRecords { get; init; }
    public int VerifiedRecords { get; init; }
    public int? FirstInvalidIndex { get; init; }
    public string? ExpectedHash { get; init; }
    public string? ActualHash { get; init; }
    public string? ErrorMessage { get; init; }

    public static HashChainVerificationResult Success(int totalRecords) => new()
    {
        IsValid = true,
        TotalRecords = totalRecords,
        VerifiedRecords = totalRecords
    };

    public static HashChainVerificationResult Failure(
        int totalRecords,
        int verifiedRecords,
        int invalidIndex,
        string expectedHash,
        string actualHash) => new()
    {
        IsValid = false,
        TotalRecords = totalRecords,
        VerifiedRecords = verifiedRecords,
        FirstInvalidIndex = invalidIndex,
        ExpectedHash = expectedHash,
        ActualHash = actualHash,
        ErrorMessage = $"Hash chain broken at index {invalidIndex}"
    };
}

/// <summary>
/// Interface for hashable records in the chain.
/// </summary>
public interface IHashableRecord
{
    /// <summary>
    /// Sequence number in the chain.
    /// </summary>
    long SequenceNumber { get; }

    /// <summary>
    /// Hash of the previous record.
    /// </summary>
    string PreviousHash { get; }

    /// <summary>
    /// Hash of this record.
    /// </summary>
    string Hash { get; }

    /// <summary>
    /// Gets the data to be hashed (excluding hash fields).
    /// </summary>
    object GetHashableData();
}
