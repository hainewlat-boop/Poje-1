using FluentAssertions;
using Platform.Security.Mfa;
using Xunit;

namespace Platform.Tests.Unit;

public class TotpServiceTests
{
    private readonly ITotpService _totpService;

    public TotpServiceTests()
    {
        _totpService = new TotpService();
    }

    [Fact]
    public void GenerateSecret_ShouldReturnValidSecret()
    {
        // Arrange
        var issuer = "Platform";
        var accountName = "user@example.com";

        // Act
        var secret = _totpService.GenerateSecret(issuer, accountName);

        // Assert
        secret.Should().NotBeNull();
        secret.Secret.Should().NotBeNullOrEmpty();
        secret.ProvisioningUri.Should().NotBeNullOrEmpty();
        secret.ManualEntryKey.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GenerateSecret_ProvisioningUri_ShouldBeValidFormat()
    {
        // Arrange
        var issuer = "Platform";
        var accountName = "user@example.com";

        // Act
        var secret = _totpService.GenerateSecret(issuer, accountName);

        // Assert
        secret.ProvisioningUri.Should().StartWith("otpauth://totp/");
        secret.ProvisioningUri.Should().Contain(Uri.EscapeDataString(issuer));
        secret.ProvisioningUri.Should().Contain("secret=");
        secret.ProvisioningUri.Should().Contain("digits=6");
        secret.ProvisioningUri.Should().Contain("period=30");
    }

    [Fact]
    public void VerifyCode_ValidCode_ShouldReturnTrue()
    {
        // Arrange
        var secret = _totpService.GenerateSecret("Platform", "user@example.com");
        var code = _totpService.GenerateCode(secret.Secret);

        // Act
        var isValid = _totpService.VerifyCode(secret.Secret, code);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void VerifyCode_InvalidCode_ShouldReturnFalse()
    {
        // Arrange
        var secret = _totpService.GenerateSecret("Platform", "user@example.com");

        // Act
        var isValid = _totpService.VerifyCode(secret.Secret, "000000");

        // Assert
        // Note: There's a tiny chance this could be the actual code
        // In a real test, we'd use a known secret and time
        isValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("12345")]
    [InlineData("1234567")]
    [InlineData("abcdef")]
    public void VerifyCode_InvalidFormat_ShouldReturnFalse(string code)
    {
        // Arrange
        var secret = _totpService.GenerateSecret("Platform", "user@example.com");

        // Act
        var isValid = _totpService.VerifyCode(secret.Secret, code);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void GenerateCode_ShouldReturnSixDigits()
    {
        // Arrange
        var secret = _totpService.GenerateSecret("Platform", "user@example.com");

        // Act
        var code = _totpService.GenerateCode(secret.Secret);

        // Assert
        code.Should().HaveLength(6);
        code.Should().MatchRegex(@"^\d{6}$");
    }
}
