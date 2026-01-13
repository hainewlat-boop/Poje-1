namespace IAM.Domain.Enums;

/// <summary>
/// User account status.
/// </summary>
public enum UserStatus
{
    /// <summary>
    /// Active user - can login and access system.
    /// </summary>
    Active = 1,

    /// <summary>
    /// Inactive user - disabled by admin.
    /// </summary>
    Inactive = 2,

    /// <summary>
    /// Locked user - too many failed login attempts.
    /// </summary>
    Locked = 3,

    /// <summary>
    /// Pending verification - awaiting email/phone verification.
    /// </summary>
    PendingVerification = 4
}
