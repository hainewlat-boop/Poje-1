using Platform.BuildingBlocks.Domain;

namespace Audit.Domain.Events;

/// <summary>
/// Domain event raised when hash chain verification completes.
/// </summary>
public record HashChainVerificationCompletedEvent(
    string ChainId,
    bool IsValid,
    long TotalRecords,
    long? FirstInvalidSequence,
    TimeSpan Duration) : DomainEvent;

/// <summary>
/// CRITICAL: Domain event raised when hash chain is broken.
/// This triggers read-only mode and security alert.
/// </summary>
public record HashChainBrokenDetectedEvent(
    string ChainId,
    long BrokenAtSequence,
    string ExpectedHash,
    string ActualHash,
    DateTime DetectedAt) : DomainEvent;

/// <summary>
/// Domain event raised when system enters read-only mode.
/// </summary>
public record ReadOnlyModeEnabledEvent(
    string Reason,
    DateTime EnabledAt) : DomainEvent;
