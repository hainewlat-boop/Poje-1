using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Platform.Security.RateLimiting;

/// <summary>
/// Rate limiting service using Redis for distributed rate limiting.
/// </summary>
public interface IRateLimiter
{
    /// <summary>
    /// Checks if the request should be allowed based on rate limits.
    /// </summary>
    Task<RateLimitResult> CheckRateLimitAsync(RateLimitRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a failed login attempt for progressive lockout.
    /// </summary>
    Task RecordFailedLoginAsync(string identifier, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears failed login attempts on successful login.
    /// </summary>
    Task ClearFailedLoginsAsync(string identifier, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an account is locked.
    /// </summary>
    Task<LockoutStatus> GetLockoutStatusAsync(string identifier, CancellationToken cancellationToken = default);
}

public record RateLimitRequest
{
    public required string Key { get; init; }
    public required RateLimitPolicy Policy { get; init; }
}

public record RateLimitPolicy
{
    public required int Limit { get; init; }
    public required TimeSpan Window { get; init; }
}

public record RateLimitResult
{
    public bool IsAllowed { get; init; }
    public int Remaining { get; init; }
    public DateTime ResetsAt { get; init; }
    public int RetryAfterSeconds { get; init; }

    public static RateLimitResult Allowed(int remaining, DateTime resetsAt) =>
        new() { IsAllowed = true, Remaining = remaining, ResetsAt = resetsAt };

    public static RateLimitResult Denied(DateTime resetsAt) =>
        new() 
        { 
            IsAllowed = false, 
            Remaining = 0, 
            ResetsAt = resetsAt,
            RetryAfterSeconds = (int)(resetsAt - DateTime.UtcNow).TotalSeconds 
        };
}

public record LockoutStatus
{
    public bool IsLocked { get; init; }
    public int FailedAttempts { get; init; }
    public DateTime? LockedUntil { get; init; }
    public TimeSpan? LockoutDuration { get; init; }
}

/// <summary>
/// Progressive lockout policy.
/// </summary>
public static class LockoutPolicy
{
    public static readonly (int Attempts, TimeSpan Duration)[] Tiers =
    {
        (3, TimeSpan.FromMinutes(1)),
        (5, TimeSpan.FromMinutes(5)),
        (7, TimeSpan.FromMinutes(15)),
        (10, TimeSpan.FromHours(24)) // Account locked - admin intervention
    };

    public static TimeSpan GetLockoutDuration(int failedAttempts)
    {
        foreach (var (attempts, duration) in Tiers)
        {
            if (failedAttempts <= attempts)
            {
                return duration;
            }
        }
        return Tiers[^1].Duration;
    }

    public static bool IsAccountLocked(int failedAttempts)
    {
        return failedAttempts >= Tiers[^1].Attempts;
    }
}

/// <summary>
/// Redis-based rate limiter implementation.
/// </summary>
public class RedisRateLimiter : IRateLimiter
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisRateLimiter> _logger;
    private const string RateLimitPrefix = "ratelimit:";
    private const string FailedLoginPrefix = "failedlogin:";
    private const string LockoutPrefix = "lockout:";

    // Lua script for atomic rate limit check and increment
    private const string RateLimitScript = @"
        local key = KEYS[1]
        local limit = tonumber(ARGV[1])
        local window = tonumber(ARGV[2])
        local current = redis.call('GET', key)
        
        if current == false then
            redis.call('SETEX', key, window, 1)
            return {1, limit - 1, window}
        end
        
        current = tonumber(current)
        if current >= limit then
            local ttl = redis.call('TTL', key)
            return {0, 0, ttl}
        end
        
        redis.call('INCR', key)
        local ttl = redis.call('TTL', key)
        return {1, limit - current - 1, ttl}
    ";

    public RedisRateLimiter(
        IConnectionMultiplexer redis,
        ILogger<RedisRateLimiter> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task<RateLimitResult> CheckRateLimitAsync(RateLimitRequest request, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var key = RateLimitPrefix + request.Key;

        var result = await db.ScriptEvaluateAsync(
            RateLimitScript,
            new RedisKey[] { key },
            new RedisValue[] { request.Policy.Limit, (int)request.Policy.Window.TotalSeconds });

        var values = (RedisResult[])result!;
        var allowed = (int)values[0] == 1;
        var remaining = (int)values[1];
        var ttl = (int)values[2];

        var resetsAt = DateTime.UtcNow.AddSeconds(ttl);

        if (!allowed)
        {
            _logger.LogWarning("Rate limit exceeded for key {Key}", request.Key);
        }

        return allowed 
            ? RateLimitResult.Allowed(remaining, resetsAt)
            : RateLimitResult.Denied(resetsAt);
    }

    public async Task RecordFailedLoginAsync(string identifier, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var key = FailedLoginPrefix + identifier;

        var count = await db.StringIncrementAsync(key);
        
        // Set expiry on first failure
        if (count == 1)
        {
            await db.KeyExpireAsync(key, TimeSpan.FromHours(24));
        }

        var lockoutDuration = LockoutPolicy.GetLockoutDuration((int)count);

        if (count >= 3) // Start locking at 3 failed attempts
        {
            var lockoutKey = LockoutPrefix + identifier;
            await db.StringSetAsync(lockoutKey, DateTime.UtcNow.Add(lockoutDuration).ToString("O"), lockoutDuration);
            
            _logger.LogWarning(
                "Account {Identifier} locked for {Duration} after {Count} failed attempts",
                identifier,
                lockoutDuration,
                count);
        }
    }

    public async Task ClearFailedLoginsAsync(string identifier, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        
        await db.KeyDeleteAsync(FailedLoginPrefix + identifier);
        await db.KeyDeleteAsync(LockoutPrefix + identifier);
    }

    public async Task<LockoutStatus> GetLockoutStatusAsync(string identifier, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        
        var failedCount = await db.StringGetAsync(FailedLoginPrefix + identifier);
        var lockoutUntil = await db.StringGetAsync(LockoutPrefix + identifier);

        var attempts = failedCount.HasValue ? (int)failedCount : 0;
        DateTime? lockedUntil = null;

        if (lockoutUntil.HasValue && DateTime.TryParse(lockoutUntil.ToString(), out var parsedDate))
        {
            if (parsedDate > DateTime.UtcNow)
            {
                lockedUntil = parsedDate;
            }
        }

        return new LockoutStatus
        {
            IsLocked = lockedUntil.HasValue,
            FailedAttempts = attempts,
            LockedUntil = lockedUntil,
            LockoutDuration = lockedUntil.HasValue ? lockedUntil - DateTime.UtcNow : null
        };
    }
}
