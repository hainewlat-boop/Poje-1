using FluentAssertions;
using IAM.Domain.Entities;
using IAM.Domain.Enums;
using IAM.Domain.ValueObjects;
using Xunit;

namespace Platform.Tests.Unit;

public class UserDomainTests
{
    [Fact]
    public void CreateUser_ShouldRaiseDomainEvent()
    {
        // Arrange
        var email = Email.Create("test@example.com");
        var name = PersonName.Create("John", "Doe");
        var passwordHash = "hashed_password";

        // Act
        var user = User.Create(email, name, passwordHash);

        // Assert
        user.Should().NotBeNull();
        user.Id.Should().NotBeEmpty();
        user.Email.Should().Be(email);
        user.Name.Should().Be(name);
        user.Status.Should().Be(UserStatus.Active);
        user.MfaEnabled.Should().BeFalse();
        user.DomainEvents.Should().HaveCount(1);
    }

    [Fact]
    public void RecordFailedLogin_ShouldIncrementAttempts()
    {
        // Arrange
        var user = CreateTestUser();

        // Act
        user.RecordFailedLogin();

        // Assert
        user.DomainEvents.Should().Contain(e => e.GetType().Name == "UserLoginFailedEvent");
    }

    [Fact]
    public void RecordFailedLogin_ThreeAttempts_ShouldLockTemporarily()
    {
        // Arrange
        var user = CreateTestUser();

        // Act
        user.RecordFailedLogin();
        user.RecordFailedLogin();
        user.RecordFailedLogin();

        // Assert
        user.IsLocked().Should().BeTrue();
    }

    [Fact]
    public void RecordSuccessfulLogin_ShouldClearLockout()
    {
        // Arrange
        var user = CreateTestUser();
        user.RecordFailedLogin();
        user.RecordFailedLogin();
        user.RecordFailedLogin();

        // Act
        user.RecordSuccessfulLogin();

        // Assert
        user.IsLocked().Should().BeFalse();
    }

    [Fact]
    public void EnableMfa_ShouldSetMfaEnabled()
    {
        // Arrange
        var user = CreateTestUser();
        var secret = "JBSWY3DPEHPK3PXP";

        // Act
        user.EnableMfa(secret);

        // Assert
        user.MfaEnabled.Should().BeTrue();
        user.DomainEvents.Should().Contain(e => e.GetType().Name == "MfaEnabledEvent");
    }

    [Fact]
    public void Deactivate_ShouldSetStatusToInactive()
    {
        // Arrange
        var user = CreateTestUser();

        // Act
        user.Deactivate();

        // Assert
        user.Status.Should().Be(UserStatus.Inactive);
        user.DomainEvents.Should().Contain(e => e.GetType().Name == "UserDeactivatedEvent");
    }

    private static User CreateTestUser()
    {
        var email = Email.Create("test@example.com");
        var name = PersonName.Create("John", "Doe");
        return User.Create(email, name, "hashed_password");
    }
}

public class EmailValueObjectTests
{
    [Fact]
    public void Create_ValidEmail_ShouldSucceed()
    {
        // Arrange & Act
        var email = Email.Create("test@example.com");

        // Assert
        email.Value.Should().Be("test@example.com");
    }

    [Fact]
    public void Create_ValidEmail_ShouldNormalizeLowercase()
    {
        // Arrange & Act
        var email = Email.Create("Test@Example.COM");

        // Assert
        email.Value.Should().Be("test@example.com");
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("invalid")]
    [InlineData("invalid@")]
    [InlineData("@example.com")]
    public void Create_InvalidEmail_ShouldThrow(string invalidEmail)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => Email.Create(invalidEmail));
    }
}

public class PhoneNumberValueObjectTests
{
    [Theory]
    [InlineData("+905551234567")]
    [InlineData("05551234567")]
    [InlineData("+1234567890123")]
    public void Create_ValidPhone_ShouldSucceed(string phone)
    {
        // Act
        var phoneNumber = PhoneNumber.Create(phone);

        // Assert
        phoneNumber.Should().NotBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("invalid")]
    public void Create_InvalidPhone_ShouldThrow(string invalidPhone)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => PhoneNumber.Create(invalidPhone));
    }
}
