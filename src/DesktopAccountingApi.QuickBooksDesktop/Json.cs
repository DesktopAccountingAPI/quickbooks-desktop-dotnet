using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DesktopAccountingApi.QuickBooksDesktop;

/// <summary>
/// The JSON settings the SDK uses on the wire. Use them to serialize SDK models yourself, for
/// example to store a response: decimals stay decimal strings with their scale, dates stay
/// <c>YYYY-MM-DD</c>, timestamps keep their offset, and input objects send only the properties you set.
/// </summary>
public static class DesktopAccountingApiJson
{
    private static readonly JsonSerializerOptions s_options = CreateOptions();

    /// <summary>Read-only serializer options matching the API's wire format.</summary>
    public static JsonSerializerOptions Options => s_options;

    /// <summary>Serializes an SDK model (or any value) to wire JSON.</summary>
    /// <typeparam name="T">The declared type of <paramref name="value"/>.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <returns>The JSON text.</returns>
    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, s_options);

    /// <summary>Deserializes wire JSON into an SDK model.</summary>
    /// <typeparam name="T">The model type.</typeparam>
    /// <param name="json">The JSON text.</param>
    /// <returns>The model, or <c>null</c> when <paramref name="json"/> is the literal <c>null</c>.</returns>
    public static T? Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, s_options);

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            NumberHandling = JsonNumberHandling.Strict,
            ReadCommentHandling = JsonCommentHandling.Disallow,
            WriteIndented = false,
        };
        options.Converters.Add(new DecimalStringConverter());
        options.Converters.Add(new DateOnlyConverter());
        options.Converters.Add(new DateTimeOffsetConverter());
        options.MakeReadOnly(populateMissingResolver: true);
        return options;
    }
}

/// <summary>Money and other decimal fields travel as strings (<c>"52.75"</c>). Parsing keeps the scale, so <c>"5.00"</c> round-trips as <c>"5.00"</c>.</summary>
internal sealed class DecimalStringConverter : JsonConverter<decimal>
{
    private const NumberStyles Styles = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent;

    public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number) return reader.GetDecimal();
        if (reader.TokenType != JsonTokenType.String) throw new JsonException($"Expected a decimal string, got {reader.TokenType}.");
        var text = reader.GetString()!;
        if (!decimal.TryParse(text, Styles, CultureInfo.InvariantCulture, out var value)) throw new JsonException($"Invalid decimal string \"{text}\".");
        return value;
    }

    public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options) =>
        writer.WriteStringValue(Format(value));

    internal static string Format(decimal value) => value.ToString(CultureInfo.InvariantCulture);
}

/// <summary>Dates travel as <c>YYYY-MM-DD</c>.</summary>
internal sealed class DateOnlyConverter : JsonConverter<DateOnly>
{
    public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String) throw new JsonException($"Expected a date string, got {reader.TokenType}.");
        var text = reader.GetString()!;
        if (!TryParse(text, out var date)) throw new JsonException($"Invalid date \"{text}\"; expected YYYY-MM-DD.");
        return date;
    }

    public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options) =>
        writer.WriteStringValue(Format(value));

    internal static string Format(DateOnly value) =>
        value.Year.ToString("D4", CultureInfo.InvariantCulture) + "-" +
        value.Month.ToString("D2", CultureInfo.InvariantCulture) + "-" +
        value.Day.ToString("D2", CultureInfo.InvariantCulture);

    internal static bool TryParse(string text, out DateOnly date)
    {
        date = default;
        if (text.Length != 10 || text[4] != '-' || text[7] != '-') return false;
        if (!int.TryParse(text.Substring(0, 4), NumberStyles.None, CultureInfo.InvariantCulture, out var y)) return false;
        if (!int.TryParse(text.Substring(5, 2), NumberStyles.None, CultureInfo.InvariantCulture, out var m)) return false;
        if (!int.TryParse(text.Substring(8, 2), NumberStyles.None, CultureInfo.InvariantCulture, out var d)) return false;
        if (y < 1 || m < 1 || m > 12 || d < 1 || d > DateTime.DaysInMonth(y, m)) return false;
        date = new DateOnly(y, m, d);
        return true;
    }
}

/// <summary>Timestamps travel as ISO 8601 with an offset. The offset QuickBooks reports is preserved; seconds are always written.</summary>
internal sealed class DateTimeOffsetConverter : JsonConverter<DateTimeOffset>
{
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String) throw new JsonException($"Expected a date-time string, got {reader.TokenType}.");
        var text = reader.GetString()!;
        if (!DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var value)) throw new JsonException($"Invalid date-time \"{text}\".");
        return value;
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options) =>
        writer.WriteStringValue(Format(value));

    internal static string Format(DateTimeOffset value)
    {
        var text = value.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture);
        var fraction = value.Ticks % TimeSpan.TicksPerSecond;
        if (fraction != 0) text += "." + fraction.ToString("D7", CultureInfo.InvariantCulture).TrimEnd('0');
        var offset = value.Offset;
        if (offset == TimeSpan.Zero) return text + "Z";
        var sign = offset < TimeSpan.Zero ? "-" : "+";
        var abs = offset.Duration();
        return text + sign + abs.Hours.ToString("D2", CultureInfo.InvariantCulture) + ":" + abs.Minutes.ToString("D2", CultureInfo.InvariantCulture);
    }
}

/// <summary>Serializes a closed enum as the wire value in its <see cref="EnumMemberAttribute"/>.</summary>
/// <typeparam name="TEnum">The enum type.</typeparam>
public sealed class WireEnumConverter<TEnum> : JsonConverter<TEnum>
    where TEnum : struct, Enum
{
    /// <inheritdoc/>
    public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String) throw new JsonException($"Expected a string for {typeof(TEnum).Name}, got {reader.TokenType}.");
        var text = reader.GetString()!;
        if (WireEnum.TryParse(typeof(TEnum), text, out var value)) return (TEnum)value;
        throw new JsonException($"\"{text}\" is not a known {typeof(TEnum).Name} value.");
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options) =>
        writer.WriteStringValue(WireEnum.ToWire(value));
}

/// <summary>Wire names of closed enums, read from <see cref="EnumMemberAttribute"/>.</summary>
internal static class WireEnum
{
    private sealed class Map
    {
        public readonly Dictionary<object, string> ToWire = new();
        public readonly Dictionary<string, object> FromWire = new(StringComparer.Ordinal);
    }

    private static readonly ConcurrentDictionary<Type, Map> s_maps = new();

    private static Map For(Type type) => s_maps.GetOrAdd(type, t =>
    {
        var map = new Map();
        foreach (var field in t.GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            var value = field.GetValue(null)!;
            var wire = field.GetCustomAttribute<EnumMemberAttribute>()?.Value ?? field.Name;
            map.ToWire[value] = wire;
            map.FromWire[wire] = value;
        }
        return map;
    });

    public static string ToWire(Enum value) =>
        For(value.GetType()).ToWire.TryGetValue(value, out var wire) ? wire : throw new DaapiException($"{value} is not a defined {value.GetType().Name} value.");

    public static bool TryParse(Type type, string wire, out object value) => For(type).FromWire.TryGetValue(wire, out value!);
}
