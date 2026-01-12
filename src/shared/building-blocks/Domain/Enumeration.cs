using System.Reflection;

namespace Platform.BuildingBlocks.Domain;

/// <summary>
/// Base class for smart enumerations.
/// Provides type-safe enumeration pattern with richer behavior than enums.
/// </summary>
public abstract class Enumeration<TEnum> : IEquatable<Enumeration<TEnum>>
    where TEnum : Enumeration<TEnum>
{
    private static readonly Lazy<Dictionary<int, TEnum>> EnumerationsByValue =
        new(() => GetEnumerations().ToDictionary(e => e.Value));

    private static readonly Lazy<Dictionary<string, TEnum>> EnumerationsByName =
        new(() => GetEnumerations().ToDictionary(e => e.Name, StringComparer.OrdinalIgnoreCase));

    protected Enumeration(int value, string name)
    {
        Value = value;
        Name = name;
    }

    public int Value { get; }
    public string Name { get; }

    public static TEnum? FromValue(int value)
    {
        return EnumerationsByValue.Value.TryGetValue(value, out var enumeration)
            ? enumeration
            : null;
    }

    public static TEnum? FromName(string name)
    {
        return EnumerationsByName.Value.TryGetValue(name, out var enumeration)
            ? enumeration
            : null;
    }

    public static IEnumerable<TEnum> GetAll() => EnumerationsByValue.Value.Values;

    public bool Equals(Enumeration<TEnum>? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return GetType() == other.GetType() && Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is Enumeration<TEnum> other && Equals(other);
    }

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Name;

    public static bool operator ==(Enumeration<TEnum>? left, Enumeration<TEnum>? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(Enumeration<TEnum>? left, Enumeration<TEnum>? right)
    {
        return !Equals(left, right);
    }

    private static IEnumerable<TEnum> GetEnumerations()
    {
        return typeof(TEnum)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(f => f.FieldType == typeof(TEnum))
            .Select(f => (TEnum)f.GetValue(null)!)
            .ToList();
    }
}
