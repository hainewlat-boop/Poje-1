using Platform.BuildingBlocks.Domain;

namespace IAM.Domain.Events;

/// <summary>
/// Domain event raised when a user is created.
/// </summary>
public record UserCreatedEvent(Guid UserId, string Email, string FullName) : DomainEvent;

/// <summary>
/// Domain event raised when a user profile is updated.
/// </summary>
public record UserUpdatedEvent(Guid UserId, string FullName) : DomainEvent;

/// <summary>
/// Domain event raised when a user's password is changed.
/// </summary>
public record PasswordChangedEvent(Guid UserId) : DomainEvent;

/// <summary>
/// Domain event raised when MFA is enabled.
/// </summary>
public record MfaEnabledEvent(Guid UserId) : DomainEvent;

/// <summary>
/// Domain event raised when MFA is disabled.
/// </summary>
public record MfaDisabledEvent(Guid UserId) : DomainEvent;

/// <summary>
/// Domain event raised on successful login.
/// </summary>
public record UserLoginSuccessEvent(Guid UserId, string Email) : DomainEvent;

/// <summary>
/// Domain event raised on failed login.
/// </summary>
public record UserLoginFailedEvent(Guid UserId, string Email, int FailedAttempts) : DomainEvent;

/// <summary>
/// Domain event raised when a user is deactivated.
/// </summary>
public record UserDeactivatedEvent(Guid UserId, string Email) : DomainEvent;

/// <summary>
/// Domain event raised when a user is activated.
/// </summary>
public record UserActivatedEvent(Guid UserId, string Email) : DomainEvent;

/// <summary>
/// Domain event raised when a role is assigned to a user.
/// </summary>
public record RoleAssignedEvent(Guid UserId, string RoleName) : DomainEvent;

/// <summary>
/// Domain event raised when a role is removed from a user.
/// </summary>
public record RoleRemovedEvent(Guid UserId, string RoleName) : DomainEvent;

/// <summary>
/// Domain event raised when a session is created.
/// </summary>
public record SessionCreatedEvent(Guid UserId, string SessionId, string IpAddress) : DomainEvent;

/// <summary>
/// Domain event raised when a session is revoked.
/// </summary>
public record SessionRevokedEvent(Guid UserId, string SessionId, string Reason) : DomainEvent;
