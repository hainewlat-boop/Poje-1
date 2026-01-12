using Microsoft.Extensions.Logging;

namespace Platform.BuildingBlocks.HashChain;

/// <summary>
/// Verifies the integrity of hash chains.
/// Used for audit and ledger verification.
/// </summary>
public interface IHashChainVerifier
{
    /// <summary>
    /// Verifies the entire hash chain.
    /// </summary>
    Task<HashChainVerificationResult> VerifyChainAsync<T>(
        IAsyncEnumerable<T> records,
        string genesisHash,
        CancellationToken cancellationToken = default) where T : IHashableRecord;

    /// <summary>
    /// Verifies a range of records in the chain.
    /// </summary>
    Task<HashChainVerificationResult> VerifyRangeAsync<T>(
        IAsyncEnumerable<T> records,
        string startHash,
        long startSequence,
        CancellationToken cancellationToken = default) where T : IHashableRecord;
}

/// <summary>
/// Implementation of hash chain verifier.
/// </summary>
public class HashChainVerifier : IHashChainVerifier
{
    private readonly IHashChainService _hashChainService;
    private readonly ILogger<HashChainVerifier> _logger;

    public HashChainVerifier(
        IHashChainService hashChainService,
        ILogger<HashChainVerifier> logger)
    {
        _hashChainService = hashChainService;
        _logger = logger;
    }

    /// <summary>
    /// Verifies the entire hash chain starting from genesis.
    /// </summary>
    public async Task<HashChainVerificationResult> VerifyChainAsync<T>(
        IAsyncEnumerable<T> records,
        string genesisHash,
        CancellationToken cancellationToken = default) where T : IHashableRecord
    {
        return await VerifyRangeAsync(records, genesisHash, 0, cancellationToken);
    }

    /// <summary>
    /// Verifies a range of records in the chain.
    /// </summary>
    public async Task<HashChainVerificationResult> VerifyRangeAsync<T>(
        IAsyncEnumerable<T> records,
        string startHash,
        long startSequence,
        CancellationToken cancellationToken = default) where T : IHashableRecord
    {
        var previousHash = startHash;
        var totalRecords = 0;
        var verifiedRecords = 0;

        await foreach (var record in records.WithCancellation(cancellationToken))
        {
            totalRecords++;

            // Verify previous hash matches
            if (!string.Equals(record.PreviousHash, previousHash, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError(
                    "Hash chain broken at sequence {SequenceNumber}: previous hash mismatch. Expected {Expected}, got {Actual}",
                    record.SequenceNumber,
                    previousHash,
                    record.PreviousHash);

                return HashChainVerificationResult.Failure(
                    totalRecords,
                    verifiedRecords,
                    (int)(record.SequenceNumber - startSequence),
                    previousHash,
                    record.PreviousHash);
            }

            // Compute expected hash
            var hashableData = record.GetHashableData();
            var computedHash = _hashChainService.ComputeHash(hashableData, previousHash);

            // Verify stored hash matches computed hash
            if (!string.Equals(record.Hash, computedHash, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError(
                    "Hash chain broken at sequence {SequenceNumber}: hash mismatch. Expected {Expected}, got {Actual}",
                    record.SequenceNumber,
                    computedHash,
                    record.Hash);

                return HashChainVerificationResult.Failure(
                    totalRecords,
                    verifiedRecords,
                    (int)(record.SequenceNumber - startSequence),
                    computedHash,
                    record.Hash);
            }

            previousHash = record.Hash;
            verifiedRecords++;

            if (verifiedRecords % 10000 == 0)
            {
                _logger.LogInformation(
                    "Hash chain verification progress: {Verified} records verified",
                    verifiedRecords);
            }
        }

        _logger.LogInformation(
            "Hash chain verification completed successfully: {Total} records verified",
            totalRecords);

        return HashChainVerificationResult.Success(totalRecords);
    }
}

/// <summary>
/// Background service for periodic hash chain verification.
/// Raises CRITICAL alert and enables read-only mode on chain break.
/// </summary>
public interface IHashChainVerificationJob
{
    /// <summary>
    /// Runs verification job.
    /// </summary>
    Task ExecuteAsync(CancellationToken cancellationToken);
}
