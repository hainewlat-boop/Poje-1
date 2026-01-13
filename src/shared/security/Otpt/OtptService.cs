using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Platform.Security.Otpt;

/// <summary>
/// One-Time-Per-Transaction (OTPT) token service.
/// Tokens are bound to: route + nonce + sessionId + fingerprintId
/// TTL: 60 seconds, one-time use, stored in Redis with write-once semantics.
/// </summary>
public interface IOtptService
{
    /// <summary>
    /// Issues a new OTPT token for a critical operation.
    /// </summary>
    Task<OtptToken> IssueTokenAsync(OtptRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Consumes (validates and invalidates) an OTPT token.
    /// Returns true if valid and consumed, false otherwise.
    /// </summary>
    Task<OtptValidationResult> ConsumeTokenAsync(OtptConsumption consumption, CancellationToken cancellationToken = default);
}

/// <summary>
/// Request to issue an OTPT token.
/// </summary>
public record OtptRequest
{
    public required string Route { get; init; }
    public required string SessionId { get; init; }
    public required string FingerprintId { get; init; }
    public required string UserId { get; init; }
    public string? Nonce { get; init; }
    public bool RequiresMfa { get; init; }
}

/// <summary>
/// OTPT token issued to the client.
/// </summary>
public record OtptToken
{
    public required string Token { get; init; }
    public required DateTime ExpiresAt { get; init; }
    public required string Nonce { get; init; }
}

/// <summary>
/// Request to consume an OTPT token.
/// </summary>
public record OtptConsumption
{
    public required string Token { get; init; }
    public required string Route { get; init; }
    public required string SessionId { get; init; }
    public required string FingerprintId { get; init; }
    public required string UserId { get; init; }
    public required string Nonce { get; init; }
}

/// <summary>
/// Result of OTPT validation.
/// </summary>
public record OtptValidationResult
{
    public bool IsValid { get; init; }
    public string? Error { get; init; }

    public static OtptValidationResult Success() => new() { IsValid = true };
    public static OtptValidationResult Failed(string error) => new() { IsValid = false, Error = error };
}

/// <summary>
/// Redis-based OTPT service implementation.
/// Uses Lua scripting for atomic consume operations.
/// </summary>
public class RedisOtptService : IOtptService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisOtptService> _logger;
    private readonly TimeSpan _tokenTtl = TimeSpan.FromSeconds(60);
    private const int TokenSize = 32;
    private const string KeyPrefix = "otpt:";

    // Lua script for atomic token consumption (write-once, consume-once)
    private const string ConsumeScript = @"
        local key = KEYS[1]
        local expectedBinding = ARGV[1]
        local value = redis.call('GET', key)
        
        if value == nil then
            return 'TOKEN_NOT_FOUND'
        end
        
        if value ~= expectedBinding then
            return 'BINDING_MISMATCH'
        end
        
        redis.call('DEL', key)
        return 'OK'
    ";

    public RedisOtptService(
        IConnectionMultiplexer redis,
        ILogger<RedisOtptService> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    /// <summary>
    /// Issues a new OTPT token.
    /// </summary>
    public async Task<OtptToken> IssueTokenAsync(OtptRequest request, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        
        var token = GenerateToken();
        var nonce = request.Nonce ?? GenerateNonce();
        var expiresAt = DateTime.UtcNow.Add(_tokenTtl);
        
        // Create binding: route|sessionId|fingerprintId|userId|nonce
        var binding = CreateBinding(request.Route, request.SessionId, request.FingerprintId, request.UserId, nonce);
        
        var key = KeyPrefix + token;
        
        // Set with NX (only if not exists) for write-once semantics
        var set = await db.StringSetAsync(key, binding, _tokenTtl, When.NotExists);
        
        if (!set)
        {
            // Extremely unlikely collision, regenerate
            return await IssueTokenAsync(request, cancellationToken);
        }

        _logger.LogInformation(
            "OTPT token issued for user {UserId} route {Route}",
            request.UserId,
            request.Route);

        return new OtptToken
        {
            Token = token,
            ExpiresAt = expiresAt,
            Nonce = nonce
        };
    }

    /// <summary>
    /// Atomically consumes an OTPT token.
    /// </summary>
    public async Task<OtptValidationResult> ConsumeTokenAsync(OtptConsumption consumption, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        
        var key = KeyPrefix + consumption.Token;
        var expectedBinding = CreateBinding(
            consumption.Route,
            consumption.SessionId,
            consumption.FingerprintId,
            consumption.UserId,
            consumption.Nonce);

        var result = await db.ScriptEvaluateAsync(
            ConsumeScript,
            new RedisKey[] { key },
            new RedisValue[] { expectedBinding });

        var resultStr = result.ToString();

        return resultStr switch
        {
            "OK" => OtptValidationResult.Success(),
            "TOKEN_NOT_FOUND" => OtptValidationResult.Failed("Token not found or expired"),
            "BINDING_MISMATCH" => OtptValidationResult.Failed("Token binding mismatch"),
            _ => OtptValidationResult.Failed("Unknown error")
        };
    }

    private static string CreateBinding(string route, string sessionId, string fingerprintId, string userId, string nonce)
    {
        return $"{route}|{sessionId}|{fingerprintId}|{userId}|{nonce}";
    }

    private static string GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(TokenSize);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }

    private static string GenerateNonce()
    {
        var bytes = RandomNumberGenerator.GetBytes(16);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
