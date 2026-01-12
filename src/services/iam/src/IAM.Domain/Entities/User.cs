using IAM.Domain.Enums;
using IAM.Domain.Events;
using IAM.Domain.ValueObjects;
using Platform.BuildingBlocks.Domain;

namespace IAM.Domain.Entities;

/// <summary>
/// User aggregate root - represents a system user.
/// </summary>
public class User : AggregateRoot
{
    private readonly List<UserRole> _userRoles = new();

    private User() { }

    public Email Email { get; private set; } = null!;
    public PersonName Name { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public EncryptedValue? NationalId { get; private set; }
    public PhoneNumber? PhoneNumber { get; private set; }
    public bool MfaEnabled { get; private set; }
    public string? MfaSecret { get; private set; }
    public UserStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    public DateTime? LockedUntil { get; private set; }
    public int FailedLoginAttempts { get; private set; }

    public IReadOnlyList<UserRole> UserRoles => _userRoles.AsReadOnly();

    public static User Create(
        Email email,
        PersonName name,
        string passwordHash,
        EncryptedValue? nationalId = null,
        PhoneNumber? phoneNumber = null)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            Name = name,
            PasswordHash = passwordHash,
            NationalId = nationalId,
            PhoneNumber = phoneNumber,
            MfaEnabled = false,
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow,
            FailedLoginAttempts = 0
        };

        user.RaiseDomainEvent(new UserCreatedEvent(user.Id, email.Value, name.FullName));

        return user;
    }

    public void UpdateProfile(PersonName name, PhoneNumber? phoneNumber)
    {
        Name = name;
        PhoneNumber = phoneNumber;
        IncrementVersion();

        RaiseDomainEvent(new UserUpdatedEvent(Id, Name.FullName));
    }

    public void ChangePassword(string newPasswordHash)
    {
        PasswordHash = newPasswordHash;
        IncrementVersion();

        RaiseDomainEvent(new PasswordChangedEvent(Id));
    }

    public void EnableMfa(string secret)
    {
        MfaSecret = secret;
        MfaEnabled = true;
        IncrementVersion();

        RaiseDomainEvent(new MfaEnabledEvent(Id));
    }

    public void DisableMfa()
    {
        MfaSecret = null;
        MfaEnabled = false;
        IncrementVersion();

        RaiseDomainEvent(new MfaDisabledEvent(Id));
    }

    public void RecordSuccessfulLogin()
    {
        LastLoginAt = DateTime.UtcNow;
        FailedLoginAttempts = 0;
        LockedUntil = null;
        IncrementVersion();

        RaiseDomainEvent(new UserLoginSuccessEvent(Id, Email.Value));
    }

    public void RecordFailedLogin()
    {
        FailedLoginAttempts++;
        IncrementVersion();

        // Progressive lockout
        if (FailedLoginAttempts >= 10)
        {
            Status = UserStatus.Locked;
            LockedUntil = DateTime.UtcNow.AddHours(24);
        }
        else if (FailedLoginAttempts >= 7)
        {
            LockedUntil = DateTime.UtcNow.AddMinutes(15);
        }
        else if (FailedLoginAttempts >= 5)
        {
            LockedUntil = DateTime.UtcNow.AddMinutes(5);
        }
        else if (FailedLoginAttempts >= 3)
        {
            LockedUntil = DateTime.UtcNow.AddMinutes(1);
        }

        RaiseDomainEvent(new UserLoginFailedEvent(Id, Email.Value, FailedLoginAttempts));
    }

    public bool IsLocked()
    {
        if (Status == UserStatus.Locked)
        {
            return true;
        }

        if (LockedUntil.HasValue && LockedUntil.Value > DateTime.UtcNow)
        {
            return true;
        }

        return false;
    }

    public void Deactivate()
    {
        Status = UserStatus.Inactive;
        IncrementVersion();

        RaiseDomainEvent(new UserDeactivatedEvent(Id, Email.Value));
    }

    public void Activate()
    {
        Status = UserStatus.Active;
        FailedLoginAttempts = 0;
        LockedUntil = null;
        IncrementVersion();

        RaiseDomainEvent(new UserActivatedEvent(Id, Email.Value));
    }

    public void AssignRole(Role role)
    {
        if (_userRoles.Any(ur => ur.RoleId == role.Id))
        {
            return; // Already has this role
        }

        var userRole = UserRole.Create(Id, role.Id);
        _userRoles.Add(userRole);
        IncrementVersion();

        RaiseDomainEvent(new RoleAssignedEvent(Id, role.Name));
    }

    public void RemoveRole(Role role)
    {
        var userRole = _userRoles.FirstOrDefault(ur => ur.RoleId == role.Id);
        if (userRole != null)
        {
            _userRoles.Remove(userRole);
            IncrementVersion();

            RaiseDomainEvent(new RoleRemovedEvent(Id, role.Name));
        }
    }
}
