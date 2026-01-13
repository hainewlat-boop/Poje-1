using System.Text.RegularExpressions;
using Platform.BuildingBlocks.Domain;

namespace IAM.Domain.ValueObjects;

/// <summary>
/// Value object representing an email address.
/// </summary>
public sealed partial class Email : ValueObject
{
    private static readonly Regex EmailRegex = GetEmailRegex();

    private Email(string value)
    {
        Value = value.ToLowerInvariant();
    }

    public string Value { get; }

    public static Email Create(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email cannot be empty", nameof(email));
        }

        email = email.Trim();

        if (!EmailRegex.IsMatch(email))
        {
            throw new ArgumentException("Invalid email format", nameof(email));
        }

        return new Email(email);
    }

    public static bool TryCreate(string email, out Email? result)
    {
        result = null;

        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        email = email.Trim();

        if (!EmailRegex.IsMatch(email))
        {
            return false;
        }

        result = new Email(email);
        return true;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(Email email) => email.Value;

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex GetEmailRegex();
}
