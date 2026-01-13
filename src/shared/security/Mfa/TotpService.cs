using System.Security.Cryptography;
using OtpNet;

namespace Platform.Security.Mfa;

/// <summary>
/// TOTP (Time-based One-Time Password) service for MFA.
/// Implements RFC 6238 with HMAC-SHA1, 6 digits, 30-second period.
/// </summary>
public interface ITotpService
{
    /// <summary>
    /// Generates a new TOTP secret for user enrollment.
    /// </summary>
    TotpSecret GenerateSecret(string issuer, string accountName);

    /// <summary>
    /// Verifies a TOTP code against a secret.
    /// Allows ±1 step tolerance for clock drift.
    /// </summary>
    bool VerifyCode(string secret, string code);

    /// <summary>
    /// Generates the current TOTP code (for testing).
    /// </summary>
    string GenerateCode(string secret);
}

/// <summary>
/// TOTP secret with QR code provisioning URI.
/// </summary>
public record TotpSecret
{
    public string Secret { get; init; } = null!;
    public string ProvisioningUri { get; init; } = null!;
    public string ManualEntryKey { get; init; } = null!;
}

/// <summary>
/// Implementation of TOTP service using OtpNet.
/// </summary>
public class TotpService : ITotpService
{
    private const int SecretSize = 20; // 160 bits
    private const int CodeDigits = 6;
    private const int PeriodSeconds = 30;
    private const int WindowTolerance = 1; // ±1 step

    /// <summary>
    /// Generates a new TOTP secret for user enrollment.
    /// </summary>
    public TotpSecret GenerateSecret(string issuer, string accountName)
    {
        ArgumentNullException.ThrowIfNull(issuer);
        ArgumentNullException.ThrowIfNull(accountName);

        var secretBytes = RandomNumberGenerator.GetBytes(SecretSize);
        var secret = Base32Encoding.ToString(secretBytes);
        
        var provisioningUri = $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(accountName)}" +
                              $"?secret={secret}&issuer={Uri.EscapeDataString(issuer)}&digits={CodeDigits}&period={PeriodSeconds}";

        return new TotpSecret
        {
            Secret = secret,
            ProvisioningUri = provisioningUri,
            ManualEntryKey = FormatForManualEntry(secret)
        };
    }

    /// <summary>
    /// Verifies a TOTP code against a secret.
    /// </summary>
    public bool VerifyCode(string secret, string code)
    {
        ArgumentNullException.ThrowIfNull(secret);
        ArgumentNullException.ThrowIfNull(code);

        if (code.Length != CodeDigits || !code.All(char.IsDigit))
        {
            return false;
        }

        try
        {
            var secretBytes = Base32Encoding.ToBytes(secret);
            var totp = new Totp(secretBytes, step: PeriodSeconds, totpSize: CodeDigits);
            
            return totp.VerifyTotp(code, out _, new VerificationWindow(WindowTolerance, WindowTolerance));
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Generates the current TOTP code.
    /// </summary>
    public string GenerateCode(string secret)
    {
        ArgumentNullException.ThrowIfNull(secret);

        var secretBytes = Base32Encoding.ToBytes(secret);
        var totp = new Totp(secretBytes, step: PeriodSeconds, totpSize: CodeDigits);
        
        return totp.ComputeTotp();
    }

    private static string FormatForManualEntry(string secret)
    {
        // Format secret in groups of 4 for easier manual entry
        return string.Join(" ", Enumerable.Range(0, (secret.Length + 3) / 4)
            .Select(i => secret.Substring(i * 4, Math.Min(4, secret.Length - i * 4))));
    }
}
