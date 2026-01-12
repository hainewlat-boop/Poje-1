using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Platform.Security.Jwt;

/// <summary>
/// JWT token generation and validation service.
/// Access tokens: 15 minutes, Refresh tokens: 7 days.
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Generates an access token.
    /// </summary>
    string GenerateAccessToken(TokenClaims claims);

    /// <summary>
    /// Validates an access token and returns the claims.
    /// </summary>
    ClaimsPrincipal? ValidateAccessToken(string token);

    /// <summary>
    /// Gets claims from an expired token (for refresh flow).
    /// </summary>
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}

/// <summary>
/// Claims to include in the token.
/// </summary>
public record TokenClaims
{
    public required string UserId { get; init; }
    public required string SessionId { get; init; }
    public required string Email { get; init; }
    public required IReadOnlyList<string> Roles { get; init; }
    public string? FingerprintId { get; init; }
    public bool MfaVerified { get; init; }
}

/// <summary>
/// JWT configuration options.
/// </summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";
    
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = "Platform";
    public string Audience { get; set; } = "Platform";
    public int AccessTokenExpiryMinutes { get; set; } = 15;
    public int RefreshTokenExpiryDays { get; set; } = 7;
}

/// <summary>
/// Implementation of JWT token service.
/// </summary>
public class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _options;
    private readonly SigningCredentials _signingCredentials;
    private readonly TokenValidationParameters _validationParameters;

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
        
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
        _signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        _validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _options.Issuer,
            ValidAudience = _options.Audience,
            IssuerSigningKey = key,
            ClockSkew = TimeSpan.Zero
        };
    }

    public string GenerateAccessToken(TokenClaims claims)
    {
        var tokenClaims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, claims.UserId),
            new(JwtRegisteredClaimNames.Email, claims.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("session_id", claims.SessionId),
            new("mfa_verified", claims.MfaVerified.ToString().ToLowerInvariant())
        };

        if (!string.IsNullOrEmpty(claims.FingerprintId))
        {
            tokenClaims.Add(new Claim("fingerprint_id", claims.FingerprintId));
        }

        foreach (var role in claims.Roles)
        {
            tokenClaims.Add(new Claim(ClaimTypes.Role, role));
        }

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: tokenClaims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(_options.AccessTokenExpiryMinutes),
            signingCredentials: _signingCredentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public ClaimsPrincipal? ValidateAccessToken(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, _validationParameters, out var validatedToken);

            if (validatedToken is not JwtSecurityToken jwtToken ||
                !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }

            return principal;
        }
        catch
        {
            return null;
        }
    }

    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = false, // Don't validate lifetime for expired tokens
            ValidateIssuerSigningKey = true,
            ValidIssuer = _options.Issuer,
            ValidAudience = _options.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey))
        };

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, validationParameters, out var validatedToken);

            if (validatedToken is not JwtSecurityToken jwtToken ||
                !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }

            return principal;
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>
/// Extension methods for claims principal.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    public static string? GetUserId(this ClaimsPrincipal principal)
    {
        return principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
    }

    public static string? GetSessionId(this ClaimsPrincipal principal)
    {
        return principal.FindFirstValue("session_id");
    }

    public static string? GetEmail(this ClaimsPrincipal principal)
    {
        return principal.FindFirstValue(JwtRegisteredClaimNames.Email);
    }

    public static string? GetFingerprintId(this ClaimsPrincipal principal)
    {
        return principal.FindFirstValue("fingerprint_id");
    }

    public static bool IsMfaVerified(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue("mfa_verified");
        return bool.TryParse(value, out var result) && result;
    }

    public static IEnumerable<string> GetRoles(this ClaimsPrincipal principal)
    {
        return principal.FindAll(ClaimTypes.Role).Select(c => c.Value);
    }
}
