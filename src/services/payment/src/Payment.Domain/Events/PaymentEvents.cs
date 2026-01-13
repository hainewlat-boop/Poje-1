using Platform.BuildingBlocks.Domain;

namespace Payment.Domain.Events;

public record PaymentIntentCreatedEvent(
    Guid PaymentIntentId,
    Guid ApplicationId,
    decimal Amount,
    string Currency) : DomainEvent;

public record PaymentIntentRedirectSetEvent(
    Guid PaymentIntentId,
    string RedirectUrl) : DomainEvent;

public record PaymentIntentExpiredEvent(Guid PaymentIntentId) : DomainEvent;

public record PaymentIntentCancelledEvent(Guid PaymentIntentId) : DomainEvent;

public record PaymentIntentFailedEvent(
    Guid PaymentIntentId,
    string Reason) : DomainEvent;

public record PaymentIntentSuspiciousEvent(
    Guid PaymentIntentId,
    string Reason) : DomainEvent;

public record PaymentCompletedEvent(
    Guid PaymentIntentId,
    Guid TransactionId,
    decimal Amount,
    string Currency,
    string? BankReferenceNumber) : DomainEvent;

public record PaymentRefundedEvent(
    Guid TransactionId,
    decimal Amount,
    string Reason) : DomainEvent;
