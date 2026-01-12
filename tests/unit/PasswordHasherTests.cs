using FluentAssertions;
using Platform.Security.Cryptography;
using Xunit;

namespace Platform.Tests.Unit;

public class PasswordHasherTests
{
    private readonly IPasswordHasher _passwordHasher;

    public PasswordHasherTests()
    {
        _passwordHasher = new CompositePasswordHasher();
    }

    [Fact]
    public void HashPassword_ShouldGenerateValidHash()
    {
        // Arrange
        var password = "SecurePassword123!@#";

        // Act
        var hash = _passwordHasher.HashPassword(password);

        // Assert
        hash.Should().NotBeNullOrEmpty();
        hash.Should().StartWith("$argon2", "Should use Argon2id");
    }

    [Fact]
    public void HashPassword_SamePassword_ShouldGenerateDifferentHashes()
    {
        // Arrange
        var password = "SecurePassword123!@#";

        // Act
        var hash1 = _passwordHasher.HashPassword(password);
        var hash2 = _passwordHasher.HashPassword(password);

        // Assert
        hash1.Should().NotBe(hash2, "Each hash should have a unique salt");
    }

    [Fact]
    public void VerifyPassword_CorrectPassword_ShouldReturnTrue()
    {
        // Arrange
        var password = "SecurePassword123!@#";
        var hash = _passwordHasher.HashPassword(password);

        // Act
        var isValid = _passwordHasher.VerifyPassword(password, hash);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_WrongPassword_ShouldReturnFalse()
    {
        // Arrange
        var password = "SecurePassword123!@#";
        var wrongPassword = "WrongPassword123!@#";
        var hash = _passwordHasher.HashPassword(password);

        // Act
        var isValid = _passwordHasher.VerifyPassword(wrongPassword, hash);

        // Assert
        isValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("short")]
    [InlineData("nouppercase123!@#")]
    [InlineData("NOLOWERCASE123!@#")]
    [InlineData("NoNumbers!@#")]
    [InlineData("NoSpecialChars123")]
    public void WeakPasswords_ShouldStillHash(string password)
    {
        // Note: Validation of password strength should be in the application layer
        // The hasher should hash any password
        if (string.IsNullOrEmpty(password))
        {
            Assert.Throws<ArgumentNullException>(() => _passwordHasher.HashPassword(password));
        }
        else
        {
            var hash = _passwordHasher.HashPassword(password);
            hash.Should().NotBeNullOrEmpty();
        }
    }
}
