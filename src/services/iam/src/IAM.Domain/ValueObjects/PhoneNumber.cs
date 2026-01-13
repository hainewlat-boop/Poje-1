using System.Text.RegularExpressions;
using Platform.BuildingBlocks.Domain;

namespace IAM.Domain.ValueObjects;

/// <summary>
/// Value object representing a phone number.
/// </summary>
public sealed partial class PhoneNumber : ValueObject
{
    private static readonly Regex PhoneRegex = GetPhoneRegex();

    private PhoneNumber(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static PhoneNumber Create(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            throw new ArgumentException("Phone number cannot be empty", nameof(phoneNumber));
        }

        // Remove all non-digit characters except leading +
        var normalized = NormalizePhoneNumber(phoneNumber);

        if (!PhoneRegex.IsMatch(normalized))
        {
            throw new ArgumentException("Invalid phone number format", nameof(phoneNumber));
        }

        return new PhoneNumber(normalized);
    }

    public static bool TryCreate(string phoneNumber, out PhoneNumber? result)
    {
        result = null;

        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return false;
        }

        var normalized = NormalizePhoneNumber(phoneNumber);

        if (!PhoneRegex.IsMatch(normalized))
        {
            return false;
        }

        result = new PhoneNumber(normalized);
        return true;
    }

    private static string NormalizePhoneNumber(string phone)
    {
        var hasPlus = phone.StartsWith("+");
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        return hasPlus ? $"+{digits}" : digits;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(PhoneNumber phone) => phone.Value;

    [GeneratedRegex(@"^\+?[0-9]{10,15}$", RegexOptions.Compiled)]
    private static partial Regex GetPhoneRegex();
}
