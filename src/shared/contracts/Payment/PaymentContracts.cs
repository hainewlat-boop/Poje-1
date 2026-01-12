namespace Platform.Contracts.Payment;

// ============================================================================
// Payment Intent
// ============================================================================

public record CreatePaymentIntentRequest
{
    public required Guid ApplicationId { get; init; }
    public required decimal Amount { get; init; }
    public required string Currency { get; init; }
    public string? Description { get; init; }
    public required string ReturnUrl { get; init; }
    public required string OtptToken { get; init; }
    public required string Nonce { get; init; }
}

public record PaymentIntentResponse
{
    public required Guid Id { get; init; }
    public required string ReferenceNumber { get; init; }
    public required decimal Amount { get; init; }
    public required string Currency { get; init; }
    public required string Status { get; init; }
    public required DateTime CreatedAt { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public string? RedirectUrl { get; init; }
}

public record InitiatePaymentResponse
{
    public required Guid PaymentIntentId { get; init; }
    public required string RedirectUrl { get; init; }
    public required DateTime ExpiresAt { get; init; }
}

// ============================================================================
// Payment Transaction
// ============================================================================

public record PaymentTransactionResponse
{
    public required Guid Id { get; init; }
    public required Guid PaymentIntentId { get; init; }
    public required decimal Amount { get; init; }
    public required string Currency { get; init; }
    public required string Status { get; init; }
    public required DateTime CreatedAt { get; init; }
    public DateTime? SettledAt { get; init; }
    public string? BankReferenceNumber { get; init; }
    public string? FailureReason { get; init; }
}

// ============================================================================
// Bank Callback (from virtual POS)
// ============================================================================

public record BankCallbackRequest
{
    public required string TransactionId { get; init; }
    public required string Status { get; init; }
    public required decimal Amount { get; init; }
    public required string Currency { get; init; }
    public required string Signature { get; init; }
    public required long Timestamp { get; init; }
    public required string Nonce { get; init; }
    public string? BankReferenceNumber { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
}

public record BankCallbackResponse
{
    public required bool Success { get; init; }
    public string? Message { get; init; }
}

// ============================================================================
// Receipt/Dekont
// ============================================================================

public record ReceiptResponse
{
    public required Guid PaymentId { get; init; }
    public required string ReceiptNumber { get; init; }
    public required decimal Amount { get; init; }
    public required string Currency { get; init; }
    public required DateTime PaymentDate { get; init; }
    public required string PayerName { get; init; }
    public string? Description { get; init; }
    public string? DownloadUrl { get; init; }
}
