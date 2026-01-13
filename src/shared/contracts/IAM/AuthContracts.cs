namespace Platform.Contracts.IAM;

// ============================================================================
// Authentication Requests/Responses
// ============================================================================

public record LoginRequest
{
    public required string Email { get; init; }
    public required string Password { get; init; }
    public required string FingerprintId { get; init; }
    public string? DeviceId { get; init; }
}

public record LoginResponse
{
    public required string AccessToken { get; init; }
    public required string RefreshToken { get; init; }
    public required DateTime AccessTokenExpiresAt { get; init; }
    public required DateTime RefreshTokenExpiresAt { get; init; }
    public required bool RequiresMfa { get; init; }
    public string? MfaToken { get; init; }
}

public record MfaVerifyRequest
{
    public required string MfaToken { get; init; }
    public required string Code { get; init; }
    public required string FingerprintId { get; init; }
}

public record RefreshTokenRequest
{
    public required string RefreshToken { get; init; }
    public required string FingerprintId { get; init; }
}

public record LogoutRequest
{
    public string? SessionId { get; init; }
    public bool LogoutAll { get; init; }
}

// ============================================================================
// MFA Enrollment
// ============================================================================

public record MfaEnrollRequest
{
    public required string Password { get; init; }
}

public record MfaEnrollResponse
{
    public required string Secret { get; init; }
    public required string QrCodeUri { get; init; }
    public required string ManualEntryKey { get; init; }
}

public record MfaConfirmRequest
{
    public required string Code { get; init; }
}

// ============================================================================
// User Management
// ============================================================================

public record CreateUserRequest
{
    public required string Email { get; init; }
    public required string Password { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public string? NationalId { get; init; }
    public string? PhoneNumber { get; init; }
    public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();
}

public record UserResponse
{
    public required Guid Id { get; init; }
    public required string Email { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public string? PhoneNumber { get; init; }
    public required bool MfaEnabled { get; init; }
    public required bool IsActive { get; init; }
    public required DateTime CreatedAt { get; init; }
    public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();
}

public record UpdateUserRequest
{
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? PhoneNumber { get; init; }
}

public record ChangePasswordRequest
{
    public required string CurrentPassword { get; init; }
    public required string NewPassword { get; init; }
}

// ============================================================================
// Role Management
// ============================================================================

public record RoleResponse
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public IReadOnlyList<string> Permissions { get; init; } = Array.Empty<string>();
}

public record CreateRoleRequest
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public IReadOnlyList<string> Permissions { get; init; } = Array.Empty<string>();
}

public record AssignRoleRequest
{
    public required Guid UserId { get; init; }
    public required string RoleName { get; init; }
}

// ============================================================================
// Session Management
// ============================================================================

public record SessionResponse
{
    public required string SessionId { get; init; }
    public required string IpAddress { get; init; }
    public required string UserAgent { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required DateTime ExpiresAt { get; init; }
    public required bool IsCurrent { get; init; }
}

// ============================================================================
// OTPT
// ============================================================================

public record OtptRequestDto
{
    public required string Route { get; init; }
    public string? Nonce { get; init; }
}

public record OtptResponseDto
{
    public required string Token { get; init; }
    public required string Nonce { get; init; }
    public required DateTime ExpiresAt { get; init; }
}

// ============================================================================
// Problem Details (RFC 7807)
// ============================================================================

public record ProblemDetails
{
    public required string Type { get; init; }
    public required string Title { get; init; }
    public required int Status { get; init; }
    public string? Detail { get; init; }
    public string? Instance { get; init; }
    public IDictionary<string, object>? Extensions { get; init; }
}
