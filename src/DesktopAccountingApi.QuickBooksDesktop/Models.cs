using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DesktopAccountingApi.QuickBooksDesktop;

/// <summary>
/// Base class of every response model. Unknown fields from newer API versions are kept in
/// <see cref="AdditionalProperties"/>, so serializing a model with
/// <see cref="DesktopAccountingApiJson.Options"/> reproduces the fields the API sent.
/// </summary>
public abstract class ApiObject
{
    /// <summary>Fields the API returned that this SDK version does not model yet.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalProperties { get; set; }

    /// <summary>Serializes this object to wire JSON.</summary>
    /// <returns>The JSON text.</returns>
    public string ToJson() => JsonSerializer.Serialize(this, GetType(), DesktopAccountingApiJson.Options);
}

/// <summary>Describes one property of an <see cref="InputObject"/>.</summary>
public sealed class InputField
{
    /// <summary>Creates a field descriptor.</summary>
    /// <param name="wire">JSON / query name.</param>
    /// <param name="name">C# property name.</param>
    /// <param name="type">Serialized type.</param>
    /// <param name="required">Whether the API requires the field.</param>
    /// <param name="nullable">Whether <c>null</c> is a value the API accepts (explicit null, often "clear").</param>
    public InputField(string wire, string name, Type type, bool required, bool nullable)
    {
        Wire = wire;
        Name = name;
        Type = type;
        Required = required;
        Nullable = nullable;
    }

    /// <summary>JSON / query name.</summary>
    public string Wire { get; }

    /// <summary>C# property name.</summary>
    public string Name { get; }

    /// <summary>Serialized type.</summary>
    public Type Type { get; }

    /// <summary>Whether the API requires the field.</summary>
    public bool Required { get; }

    /// <summary>Whether the API accepts an explicit <c>null</c>.</summary>
    public bool Nullable { get; }
}

/// <summary>
/// Base class of request bodies and query parameter objects. It records which properties you
/// set: only those are sent. Setting a nullable property (marked "Set to null to clear") to
/// <c>null</c> sends an explicit <c>null</c>; setting any other property to <c>null</c> removes it
/// from the request again.
/// </summary>
public abstract class InputObject
{
    private readonly InputField[] _fields;
    private readonly object?[] _values;
    private readonly bool[] _set;

    /// <summary>Initializes the property storage.</summary>
    /// <param name="fields">The generated field table.</param>
    protected InputObject(InputField[] fields)
    {
        _fields = fields ?? throw new ArgumentNullException(nameof(fields));
        _values = new object?[fields.Length];
        _set = new bool[fields.Length];
    }

    /// <summary>Reads a property value.</summary>
    /// <typeparam name="T">The property type.</typeparam>
    /// <param name="index">Field index.</param>
    /// <returns>The value, or the default when unset.</returns>
    protected T? GetField<T>(int index) => _values[index] is { } value ? (T)value : default;

    /// <summary>Sets a property value and marks it as sent (see the class remarks for <c>null</c>).</summary>
    /// <param name="index">Field index.</param>
    /// <param name="value">The value.</param>
    protected void SetField(int index, object? value)
    {
        _values[index] = value;
        _set[index] = value is not null || _fields[index].Nullable;
    }

    /// <summary>Whether a property will be sent (it was set, possibly to an explicit <c>null</c>).</summary>
    /// <param name="name">C# property name or wire name.</param>
    /// <returns><c>true</c> when the property is set.</returns>
    public bool IsSet(string name) => _set[IndexOf(name)];

    /// <summary>Removes a property from the request.</summary>
    /// <param name="name">C# property name or wire name.</param>
    public void Unset(string name)
    {
        var i = IndexOf(name);
        _set[i] = false;
        _values[i] = null;
    }

    private int IndexOf(string name)
    {
        for (var i = 0; i < _fields.Length; i++)
        {
            if (_fields[i].Wire == name || _fields[i].Name == name) return i;
        }
        throw new ArgumentException($"{GetType().Name} has no property {name}.", nameof(name));
    }

    internal void WriteJson(Utf8JsonWriter writer, JsonSerializerOptions options)
    {
        CheckRequired();
        writer.WriteStartObject();
        for (var i = 0; i < _fields.Length; i++)
        {
            if (!_set[i]) continue;
            writer.WritePropertyName(_fields[i].Wire);
            if (_values[i] is { } value) JsonSerializer.Serialize(writer, value, _fields[i].Type, options);
            else writer.WriteNullValue();
        }
        writer.WriteEndObject();
    }

    internal void ReadJson(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException($"Expected an object for {GetType().Name}.");
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject) return;
            var name = reader.GetString()!;
            var index = -1;
            for (var i = 0; i < _fields.Length; i++)
            {
                if (_fields[i].Wire == name) index = i;
            }
            if (index < 0) throw new JsonException($"{GetType().Name} has no field \"{name}\".");
            reader.Read();
            if (reader.TokenType == JsonTokenType.Null)
            {
                _values[index] = null;
                _set[index] = true;
                continue;
            }
            _values[index] = JsonSerializer.Deserialize(ref reader, _fields[index].Type, options);
            _set[index] = true;
        }
        throw new JsonException("Unexpected end of JSON.");
    }

    internal List<KeyValuePair<string, string>> ToQuery()
    {
        CheckRequired();
        var pairs = new List<KeyValuePair<string, string>>();
        for (var i = 0; i < _fields.Length; i++)
        {
            if (!_set[i] || _values[i] is not { } value) continue;
            if (value is not string && value is IEnumerable items)
            {
                foreach (var item in items)
                {
                    if (item is not null) pairs.Add(new KeyValuePair<string, string>(_fields[i].Wire, FormatQueryValue(item)));
                }
            }
            else
            {
                pairs.Add(new KeyValuePair<string, string>(_fields[i].Wire, FormatQueryValue(value)));
            }
        }
        return pairs;
    }

    internal string? QueryValue(string wire)
    {
        for (var i = 0; i < _fields.Length; i++)
        {
            if (_fields[i].Wire == wire && _set[i] && _values[i] is { } value) return FormatQueryValue(value);
        }
        return null;
    }

    internal static string FormatQueryValue(object value) => value switch
    {
        string s => s,
        bool b => b ? "true" : "false",
        int n => n.ToString(CultureInfo.InvariantCulture),
        long n => n.ToString(CultureInfo.InvariantCulture),
        double d => d.ToString("R", CultureInfo.InvariantCulture),
        decimal m => DecimalStringConverter.Format(m),
        DateOnly d => DateOnlyConverter.Format(d),
        DateTimeOffset t => DateTimeOffsetConverter.Format(t),
        Enum e => WireEnum.ToWire(e),
        _ => throw new DaapiException($"Cannot send a {value.GetType().Name} as a query parameter."),
    };

    private void CheckRequired()
    {
        for (var i = 0; i < _fields.Length; i++)
        {
            if (_fields[i].Required && !_set[i]) throw new DaapiException($"{GetType().Name}.{_fields[i].Name} is required.");
        }
    }
}

/// <summary>JSON converter for <see cref="InputObject"/> types: writes only the properties that are set.</summary>
public sealed class InputObjectConverter : JsonConverterFactory
{
    /// <inheritdoc/>
    public override bool CanConvert(Type typeToConvert) => typeof(InputObject).IsAssignableFrom(typeToConvert) && !typeToConvert.IsAbstract;

    /// <inheritdoc/>
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
        (JsonConverter)Activator.CreateInstance(typeof(Converter<>).MakeGenericType(typeToConvert))!;

    private sealed class Converter<T> : JsonConverter<T>
        where T : InputObject
    {
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = (T)Activator.CreateInstance(typeToConvert)!;
            value.ReadJson(ref reader, options);
            return value;
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) => value.WriteJson(writer, options);
    }
}
