namespace Payment.Domain.Enums;

/// <summary>
/// Payment intent status.
/// </summary>
public enum PaymentIntentStatus
{
    /// <summary>
    /// Intent created, waiting for bank redirect URL.
    /// </summary>
    Created = 1,

    /// <summary>
    /// Redirect URL set, waiting for user to complete payment.
    /// </summary>
    Pending = 2,

    /// <summary>
    /// Payment completed successfully.
    /// </summary>
    Completed = 3,

    /// <summary>
    /// Payment failed.
    /// </summary>
    Failed = 4,

    /// <summary>
    /// Payment cancelled by user.
    /// </summary>
    Cancelled = 5,

    /// <summary>
    /// Payment expired (timeout).
    /// </summary>
    Expired = 6,

    /// <summary>
    /// Suspicious activity detected - locked for review.
    /// </summary>
    Suspicious = 7
}

/// <summary>
/// Payment transaction status.
/// </summary>
public enum PaymentTransactionStatus
{
    /// <summary>
    /// Transaction pending bank confirmation.
    /// </summary>
    Pending = 1,

    /// <summary>
    /// Transaction settled by bank.
    /// </summary>
    Settled = 2,

    /// <summary>
    /// Transaction failed.
    /// </summary>
    Failed = 3,

    /// <summary>
    /// Transaction refunded.
    /// </summary>
    Refunded = 4
}
