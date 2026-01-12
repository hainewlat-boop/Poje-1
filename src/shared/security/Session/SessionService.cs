using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Platform.Security.Session;

/// <summary>
/// Session management service with fingerprint binding.
/// Sessions are bound to FingerprintID + SessionID.
/// Mismatch triggers immediate session revocation.
/// </summary>
public interface ISessionService
{
    /// <summary>
    /// Creates a new session.
    /// </summary>
    Task<SessionInfo> CreateSessionAsync(CreateSessionRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a session and its fingerprint binding.
    /// </summary>
    Task<SessionValidationResult> ValidateSessionAsync(ValidateSessionRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes a session.
    /// </summary>
    Task RevokeSessionAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes all sessions for a user.
    /// </summary>
    Task RevokeAllUserSessionsAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes a session, returning new tokens.
    /// </summary>
    Task<SessionRefreshResult> RefreshSessionAsync(RefreshSessionRequest request, CancellationToken cancellationToken = default);
}

public record CreateSessionRequest
{
    public required string UserId { get; init; }
    public required string FingerprintId { get; init; }
    public required string IpAddress { get; init; }
    public required string UserAgent { get; init; }
    public string? DeviceId { get; init; }
}

public record SessionInfo
{
    public required string SessionId { get; init; }
    public required string RefreshToken { get; init; }
    public required DateTime ExpiresAt { get; init; }
    public required DateTime RefreshExpiresAt { get; init; }
}

public record ValidateSessionRequest
{
    public required string SessionId { get; init; }
    public required string FingerprintId { get; init; }
    public required string UserId { get; init; }
}

public record SessionValidationResult
{
    public bool IsValid { get; init; }
    public string? Error { get; init; }
    public bool FingerprintMismatch { get; init; }

    public static SessionValidationResult Success() => new() { IsValid = true };
    public static SessionValidationResult Invalid(string error, bool fingerprintMismatch = false) => 
        new() { IsValid = false, Error = error, FingerprintMismatch = fingerprintMismatch };
}

public record RefreshSessionRequest
{
    public required string RefreshToken { get; init; }
    public required string FingerprintId { get; init; }
}

public record SessionRefreshResult
{
    public bool IsSuccess { get; init; }
    public string? Error { get; init; }
    public SessionInfo? NewSession { get; init; }

    public static SessionRefreshResult Success(SessionInfo session) => 
        new() { IsSuccess = true, NewSession = session };
    public static SessionRefreshResult Failed(string error) => 
        new() { IsSuccess = false, Error = error };
}

/// <summary>
/// Redis-based session service implementation.
/// </summary>
public class RedisSessionService : ISessionService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisSessionService> _logger;
    private readonly TimeSpan _sessionTtl = TimeSpan.FromMinutes(15);
    private readonly TimeSpan _refreshTokenTtl = TimeSpan.FromDays(7);
    private const string SessionKeyPrefix = "session:";
    private const string RefreshKeyPrefix = "refresh:";
    private const string UserSessionsKeyPrefix = "user_sessions:";

    public RedisSessionService(
        IConnectionMultiplexer redis,
        ILogger<RedisSessionService> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task<SessionInfo> CreateSessionAsync(CreateSessionRequest request, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        
        var sessionId = GenerateSessionId();
        var refreshToken = GenerateRefreshToken();
        var now = DateTime.UtcNow;
        var sessionExpiry = now.Add(_sessionTtl);
        var refreshExpiry = now.Add(_refreshTokenTtl);

        var sessionData = new SessionData
        {
            SessionId = sessionId,
            UserId = request.UserId,
            FingerprintId = request.FingerprintId,
            IpAddress = request.IpAddress,
            UserAgent = request.UserAgent,
            DeviceId = request.DeviceId,
            CreatedAt = now,
            ExpiresAt = sessionExpiry
        };

        var refreshData = new RefreshTokenData
        {
            RefreshToken = refreshToken,
            SessionId = sessionId,
            UserId = request.UserId,
            FingerprintId = request.FingerprintId,
            ExpiresAt = refreshExpiry
        };

        // Store session
        await db.StringSetAsync(
            SessionKeyPrefix + sessionId,
            System.Text.Json.JsonSerializer.Serialize(sessionData),
            _sessionTtl);

        // Store refresh token
        await db.StringSetAsync(
            RefreshKeyPrefix + refreshToken,
            System.Text.Json.JsonSerializer.Serialize(refreshData),
            _refreshTokenTtl);

        // Track user sessions
        await db.SetAddAsync(UserSessionsKeyPrefix + request.UserId, sessionId);

        _logger.LogInformation(
            "Session created for user {UserId}, session {SessionId}",
            request.UserId,
            sessionId);

        return new SessionInfo
        {
            SessionId = sessionId,
            RefreshToken = refreshToken,
            ExpiresAt = sessionExpiry,
            RefreshExpiresAt = refreshExpiry
        };
    }

    public async Task<SessionValidationResult> ValidateSessionAsync(ValidateSessionRequest request, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        
        var sessionJson = await db.StringGetAsync(SessionKeyPrefix + request.SessionId);
        
        if (sessionJson.IsNullOrEmpty)
        {
            return SessionValidationResult.Invalid("Session not found or expired");
        }

        var sessionData = System.Text.Json.JsonSerializer.Deserialize<SessionData>(sessionJson!);
        
        if (sessionData == null)
        {
            return SessionValidationResult.Invalid("Invalid session data");
        }

        if (sessionData.UserId != request.UserId)
        {
            return SessionValidationResult.Invalid("User ID mismatch");
        }

        // CRITICAL: Fingerprint validation
        if (sessionData.FingerprintId != request.FingerprintId)
        {
            _logger.LogWarning(
                "Fingerprint mismatch for session {SessionId}, user {UserId}. Expected {Expected}, got {Actual}",
                request.SessionId,
                request.UserId,
                sessionData.FingerprintId,
                request.FingerprintId);

            // Revoke the session immediately
            await RevokeSessionAsync(request.SessionId, cancellationToken);
            
            return SessionValidationResult.Invalid("Session invalidated due to fingerprint mismatch", fingerprintMismatch: true);
        }

        return SessionValidationResult.Success();
    }

    public async Task RevokeSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        
        var sessionJson = await db.StringGetAsync(SessionKeyPrefix + sessionId);
        
        if (!sessionJson.IsNullOrEmpty)
        {
            var sessionData = System.Text.Json.JsonSerializer.Deserialize<SessionData>(sessionJson!);
            if (sessionData != null)
            {
                await db.SetRemoveAsync(UserSessionsKeyPrefix + sessionData.UserId, sessionId);
            }
        }

        await db.KeyDeleteAsync(SessionKeyPrefix + sessionId);
        
        _logger.LogInformation("Session {SessionId} revoked", sessionId);
    }

    public async Task RevokeAllUserSessionsAsync(string userId, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        
        var sessionIds = await db.SetMembersAsync(UserSessionsKeyPrefix + userId);
        
        foreach (var sessionId in sessionIds)
        {
            await db.KeyDeleteAsync(SessionKeyPrefix + sessionId.ToString());
        }

        await db.KeyDeleteAsync(UserSessionsKeyPrefix + userId);
        
        _logger.LogInformation("All sessions revoked for user {UserId}", userId);
    }

    public async Task<SessionRefreshResult> RefreshSessionAsync(RefreshSessionRequest request, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        
        var refreshJson = await db.StringGetAsync(RefreshKeyPrefix + request.RefreshToken);
        
        if (refreshJson.IsNullOrEmpty)
        {
            return SessionRefreshResult.Failed("Refresh token not found or expired");
        }

        var refreshData = System.Text.Json.JsonSerializer.Deserialize<RefreshTokenData>(refreshJson!);
        
        if (refreshData == null)
        {
            return SessionRefreshResult.Failed("Invalid refresh token data");
        }

        // Validate fingerprint
        if (refreshData.FingerprintId != request.FingerprintId)
        {
            _logger.LogWarning(
                "Fingerprint mismatch on refresh for user {UserId}",
                refreshData.UserId);

            // Revoke all user sessions on fingerprint mismatch during refresh
            await RevokeAllUserSessionsAsync(refreshData.UserId, cancellationToken);
            await db.KeyDeleteAsync(RefreshKeyPrefix + request.RefreshToken);
            
            return SessionRefreshResult.Failed("Session invalidated due to fingerprint mismatch");
        }

        // Delete old refresh token (rotation)
        await db.KeyDeleteAsync(RefreshKeyPrefix + request.RefreshToken);
        await RevokeSessionAsync(refreshData.SessionId, cancellationToken);

        // Create new session
        var newSession = await CreateSessionAsync(new CreateSessionRequest
        {
            UserId = refreshData.UserId,
            FingerprintId = refreshData.FingerprintId,
            IpAddress = "refresh", // Would need to pass this through
            UserAgent = "refresh"
        }, cancellationToken);

        return SessionRefreshResult.Success(newSession);
    }

    private static string GenerateSessionId()
    {
        var bytes = RandomNumberGenerator.GetBytes(24);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }

    private static string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }

    private record SessionData
    {
        public string SessionId { get; init; } = null!;
        public string UserId { get; init; } = null!;
        public string FingerprintId { get; init; } = null!;
        public string IpAddress { get; init; } = null!;
        public string UserAgent { get; init; } = null!;
        public string? DeviceId { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime ExpiresAt { get; init; }
    }

    private record RefreshTokenData
    {
        public string RefreshToken { get; init; } = null!;
        public string SessionId { get; init; } = null!;
        public string UserId { get; init; } = null!;
        public string FingerprintId { get; init; } = null!;
        public DateTime ExpiresAt { get; init; }
    }
}
