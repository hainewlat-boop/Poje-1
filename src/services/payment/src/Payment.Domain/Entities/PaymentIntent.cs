using Payment.Domain.Enums;
using Payment.Domain.Events;
using Platform.BuildingBlocks.Domain;

namespace Payment.Domain.Entities;

/// <summary>
/// Payment Intent aggregate root.
/// Represents the intention to make a payment.
/// NOTE: NO CARD DATA (PAN/CVV) IS EVER STORED.
/// </summary>
public class PaymentIntent : AggregateRoot
{
    private PaymentIntent() { }

    public Guid ApplicationId { get; private set; }
    public string ReferenceNumber { get; private set; } = null!;
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = null!;
    public string? Description { get; private set; }
    public PaymentIntentStatus Status { get; private set; }
    public string ReturnUrl { get; private set; } = null!;
    public string? BankRedirectUrl { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public Guid CreatedByUserId { get; private set; }

    public static PaymentIntent Create(
        Guid applicationId,
        decimal amount,
        string currency,
        string returnUrl,
        Guid createdByUserId,
        string? description = null)
    {
        if (amount <= 0)
        {
            throw new ArgumentException("Amount must be positive", nameof(amount));
        }

        var intent = new PaymentIntent
        {
            Id = Guid.NewGuid(),
            ApplicationId = applicationId,
            ReferenceNumber = GenerateReferenceNumber(),
            Amount = amount,
            Currency = currency.ToUpperInvariant(),
            Description = description,
            ReturnUrl = returnUrl,
            Status = PaymentIntentStatus.Created,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(1), // 1 hour expiry
            CreatedByUserId = createdByUserId
        };

        intent.RaiseDomainEvent(new PaymentIntentCreatedEvent(
            intent.Id,
            intent.ApplicationId,
            intent.Amount,
            intent.Currency));

        return intent;
    }

    public void SetBankRedirectUrl(string redirectUrl)
    {
        BankRedirectUrl = redirectUrl;
        Status = PaymentIntentStatus.Pending;
        IncrementVersion();

        RaiseDomainEvent(new PaymentIntentRedirectSetEvent(Id, redirectUrl));
    }

    public void MarkAsExpired()
    {
        if (Status == PaymentIntentStatus.Completed)
        {
            throw new InvalidOperationException("Cannot expire a completed payment");
        }

        Status = PaymentIntentStatus.Expired;
        IncrementVersion();

        RaiseDomainEvent(new PaymentIntentExpiredEvent(Id));
    }

    public void MarkAsCancelled()
    {
        if (Status == PaymentIntentStatus.Completed)
        {
            throw new InvalidOperationException("Cannot cancel a completed payment");
        }

        Status = PaymentIntentStatus.Cancelled;
        IncrementVersion();

        RaiseDomainEvent(new PaymentIntentCancelledEvent(Id));
    }

    public void MarkAsCompleted()
    {
        Status = PaymentIntentStatus.Completed;
        IncrementVersion();
    }

    public void MarkAsFailed(string reason)
    {
        Status = PaymentIntentStatus.Failed;
        IncrementVersion();

        RaiseDomainEvent(new PaymentIntentFailedEvent(Id, reason));
    }

    public void MarkAsSuspicious(string reason)
    {
        Status = PaymentIntentStatus.Suspicious;
        IncrementVersion();

        RaiseDomainEvent(new PaymentIntentSuspiciousEvent(Id, reason));
    }

    public bool IsExpired => ExpiresAt.HasValue && DateTime.UtcNow > ExpiresAt.Value;

    private static string GenerateReferenceNumber()
    {
        return $"PAY-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
    }
}
