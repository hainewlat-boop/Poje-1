using Platform.BuildingBlocks.Domain;

namespace IAM.Domain.ValueObjects;

/// <summary>
/// Value object representing a person's name.
/// </summary>
public sealed class PersonName : ValueObject
{
    private PersonName(string firstName, string lastName)
    {
        FirstName = firstName;
        LastName = lastName;
    }

    public string FirstName { get; }
    public string LastName { get; }
    public string FullName => $"{FirstName} {LastName}";

    public static PersonName Create(string firstName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(firstName))
        {
            throw new ArgumentException("First name cannot be empty", nameof(firstName));
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            throw new ArgumentException("Last name cannot be empty", nameof(lastName));
        }

        return new PersonName(firstName.Trim(), lastName.Trim());
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return FirstName;
        yield return LastName;
    }

    public override string ToString() => FullName;
}
