namespace Platform.Contracts.Audit;

// ============================================================================
// Audit Events
// ============================================================================

public record AuditEventResponse
{
    public required Guid Id { get; init; }
    public required long SequenceNumber { get; init; }
    public required string EventType { get; init; }
    public required string Action { get; init; }
    public required string Resource { get; init; }
    public Guid? ResourceId { get; init; }
    public required Guid UserId { get; init; }
    public string? UserEmail { get; init; }
    public required string IpAddress { get; init; }
    public required DateTime Timestamp { get; init; }
    public required string Hash { get; init; }
    public required string PreviousHash { get; init; }
    public IDictionary<string, object>? Details { get; init; }
}

public record AuditSearchRequest
{
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public string? EventType { get; init; }
    public string? Resource { get; init; }
    public Guid? UserId { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
}

// ============================================================================
// Hash Chain Verification
// ============================================================================

public record HashChainStatusResponse
{
    public required bool IsValid { get; init; }
    public required long TotalRecords { get; init; }
    public required DateTime LastVerifiedAt { get; init; }
    public long? FirstInvalidSequence { get; init; }
    public string? ErrorMessage { get; init; }
}

public record VerifyHashChainRequest
{
    public long? FromSequence { get; init; }
    public long? ToSequence { get; init; }
}

public record VerifyHashChainResponse
{
    public required bool IsValid { get; init; }
    public required long RecordsVerified { get; init; }
    public required TimeSpan Duration { get; init; }
    public long? FirstInvalidSequence { get; init; }
    public string? ExpectedHash { get; init; }
    public string? ActualHash { get; init; }
}

// ============================================================================
// Audit Event Types
// ============================================================================

public static class AuditEventTypes
{
    // Authentication
    public const string LoginSuccess = "auth.login.success";
    public const string LoginFailed = "auth.login.failed";
    public const string Logout = "auth.logout";
    public const string MfaEnabled = "auth.mfa.enabled";
    public const string MfaVerified = "auth.mfa.verified";
    public const string SessionCreated = "auth.session.created";
    public const string SessionRevoked = "auth.session.revoked";
    public const string TokenRefreshed = "auth.token.refreshed";
    
    // User Management
    public const string UserCreated = "user.created";
    public const string UserUpdated = "user.updated";
    public const string UserDeactivated = "user.deactivated";
    public const string PasswordChanged = "user.password.changed";
    public const string RoleAssigned = "user.role.assigned";
    public const string RoleRevoked = "user.role.revoked";
    
    // Application
    public const string ApplicationCreated = "application.created";
    public const string ApplicationSubmitted = "application.submitted";
    public const string ApplicationApproved = "application.approved";
    public const string ApplicationRejected = "application.rejected";
    public const string TaskAssigned = "application.task.assigned";
    public const string TaskCompleted = "application.task.completed";
    public const string AttachmentUploaded = "application.attachment.uploaded";
    
    // Payment
    public const string PaymentIntentCreated = "payment.intent.created";
    public const string PaymentInitiated = "payment.initiated";
    public const string PaymentCompleted = "payment.completed";
    public const string PaymentFailed = "payment.failed";
    public const string PaymentRefunded = "payment.refunded";
    
    // Document
    public const string DocumentGenerated = "document.generated";
    public const string DocumentDownloaded = "document.downloaded";
    
    // System
    public const string HashChainVerified = "system.hashchain.verified";
    public const string HashChainBroken = "system.hashchain.broken";
    public const string ReadOnlyModeEnabled = "system.readonly.enabled";
}
