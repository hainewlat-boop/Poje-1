using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Platform.BuildingBlocks.HashChain;

/// <summary>
/// Deterministic JSON serializer for hash chain computation.
/// Produces canonical JSON with:
/// - Sorted keys alphabetically
/// - No whitespace
/// - Dates in ISO 8601 UTC format
/// - Numbers without trailing zeros
/// - Null values excluded
/// </summary>
public static class CanonicalJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters =
        {
            new CanonicalDateTimeConverter(),
            new CanonicalDecimalConverter()
        }
    };

    /// <summary>
    /// Serializes an object to canonical JSON string.
    /// </summary>
    public static string Serialize<T>(T value) where T : class
    {
        var json = JsonSerializer.Serialize(value, Options);
        return SortJsonKeys(json);
    }

    /// <summary>
    /// Serializes an object to canonical JSON bytes (UTF-8).
    /// </summary>
    public static byte[] SerializeToBytes<T>(T value) where T : class
    {
        return Encoding.UTF8.GetBytes(Serialize(value));
    }

    /// <summary>
    /// Sorts JSON object keys alphabetically for deterministic output.
    /// </summary>
    private static string SortJsonKeys(string json)
    {
        using var document = JsonDocument.Parse(json);
        return SerializeElement(document.RootElement);
    }

    private static string SerializeElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => SerializeObject(element),
            JsonValueKind.Array => SerializeArray(element),
            JsonValueKind.String => JsonSerializer.Serialize(element.GetString()),
            JsonValueKind.Number => SerializeNumber(element),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => "null",
            _ => element.GetRawText()
        };
    }

    private static string SerializeObject(JsonElement element)
    {
        var properties = element.EnumerateObject()
            .Where(p => p.Value.ValueKind != JsonValueKind.Null)
            .OrderBy(p => p.Name, StringComparer.Ordinal)
            .Select(p => $"\"{p.Name}\":{SerializeElement(p.Value)}");

        return "{" + string.Join(",", properties) + "}";
    }

    private static string SerializeArray(JsonElement element)
    {
        var items = element.EnumerateArray()
            .Select(SerializeElement);

        return "[" + string.Join(",", items) + "]";
    }

    private static string SerializeNumber(JsonElement element)
    {
        if (element.TryGetInt64(out var longValue))
        {
            return longValue.ToString(CultureInfo.InvariantCulture);
        }

        if (element.TryGetDecimal(out var decimalValue))
        {
            // Remove trailing zeros for canonical representation
            return decimalValue.ToString("G", CultureInfo.InvariantCulture);
        }

        return element.GetDouble().ToString("G", CultureInfo.InvariantCulture);
    }
}

/// <summary>
/// Custom converter for DateTime to ensure ISO 8601 UTC format.
/// </summary>
public class CanonicalDateTimeConverter : JsonConverter<DateTime>
{
    private const string Format = "yyyy-MM-ddTHH:mm:ss.fffZ";

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return DateTime.Parse(reader.GetString()!, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToUniversalTime().ToString(Format, CultureInfo.InvariantCulture));
    }
}

/// <summary>
/// Custom converter for decimal to remove trailing zeros.
/// </summary>
public class CanonicalDecimalConverter : JsonConverter<decimal>
{
    public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.GetDecimal();
    }

    public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
    {
        // Normalize to remove trailing zeros
        var normalized = value / 1.000000000000000000000000000000000m;
        writer.WriteRawValue(normalized.ToString("G", CultureInfo.InvariantCulture));
    }
}

/// <summary>
/// Custom converter for DateTimeOffset to ensure ISO 8601 UTC format.
/// </summary>
public class CanonicalDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
{
    private const string Format = "yyyy-MM-ddTHH:mm:ss.fffZ";

    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return DateTimeOffset.Parse(reader.GetString()!, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToUniversalTime().ToString(Format, CultureInfo.InvariantCulture));
    }
}
