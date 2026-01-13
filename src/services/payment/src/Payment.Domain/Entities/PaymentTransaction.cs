using Payment.Domain.Enums;
using Payment.Domain.Events;
using Platform.BuildingBlocks.Domain;

namespace Payment.Domain.Entities;

/// <summary>
/// Payment Transaction entity.
/// Records the result of a payment attempt from the bank.
/// </summary>
public class PaymentTransaction : Entity
{
    private PaymentTransaction() { }

    public Guid PaymentIntentId { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = null!;
    public PaymentTransactionStatus Status { get; private set; }
    public string? BankReferenceNumber { get; private set; }
    public string? BankTransactionId { get; private set; }
    public string? FailureCode { get; private set; }
    public string? FailureMessage { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? SettledAt { get; private set; }

    /// <summary>
    /// Idempotency key to prevent duplicate processing.
    /// </summary>
    public string IdempotencyKey { get; private set; } = null!;

    public static PaymentTransaction CreateFromCallback(
        Guid paymentIntentId,
        decimal amount,
        string currency,
        string idempotencyKey,
        string? bankReferenceNumber = null,
        string? bankTransactionId = null)
    {
        return new PaymentTransaction
        {
            Id = Guid.NewGuid(),
            PaymentIntentId = paymentIntentId,
            Amount = amount,
            Currency = currency,
            Status = PaymentTransactionStatus.Pending,
            BankReferenceNumber = bankReferenceNumber,
            BankTransactionId = bankTransactionId,
            IdempotencyKey = idempotencyKey,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void MarkAsSettled()
    {
        Status = PaymentTransactionStatus.Settled;
        SettledAt = DateTime.UtcNow;
    }

    public void MarkAsFailed(string failureCode, string failureMessage)
    {
        Status = PaymentTransactionStatus.Failed;
        FailureCode = failureCode;
        FailureMessage = failureMessage;
    }

    public void MarkAsRefunded()
    {
        Status = PaymentTransactionStatus.Refunded;
    }
}

/// <summary>
/// Stores processed idempotency keys to prevent duplicate callbacks.
/// </summary>
public class IdempotencyRecord : Entity
{
    private IdempotencyRecord() { }

    public string Key { get; private set; } = null!;
    public Guid PaymentIntentId { get; private set; }
    public DateTime ProcessedAt { get; private set; }
    public string? ResponseHash { get; private set; }

    public static IdempotencyRecord Create(string key, Guid paymentIntentId, string? responseHash = null)
    {
        return new IdempotencyRecord
        {
            Id = Guid.NewGuid(),
            Key = key,
            PaymentIntentId = paymentIntentId,
            ProcessedAt = DateTime.UtcNow,
            ResponseHash = responseHash
        };
    }
}
