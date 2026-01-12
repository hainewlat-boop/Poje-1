using Platform.BuildingBlocks.Domain;

namespace Ledger.Domain.Events;

public record LedgerEntryCreatedEvent(
    Guid EntryId,
    long SequenceNumber,
    Guid ReferenceId,
    decimal Amount,
    string Currency) : DomainEvent;

public record LedgerReversalCreatedEvent(
    Guid ReversalEntryId,
    Guid OriginalEntryId,
    decimal Amount,
    string Reason) : DomainEvent;

public record HashChainVerifiedEvent(
    long FromSequence,
    long ToSequence,
    bool IsValid,
    DateTime VerifiedAt) : DomainEvent;

public record HashChainBrokenEvent(
    long AtSequence,
    string ExpectedHash,
    string ActualHash,
    DateTime DetectedAt) : DomainEvent;
