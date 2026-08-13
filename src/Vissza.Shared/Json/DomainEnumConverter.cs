using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Vissza.Shared.Json;

/// <summary>
/// Az enum azon neve, ahogy a JSON-ban és a hibaüzenetben szerepel. Nem
/// mindig vezethető le a típusnévből: az OfferStatus a dróton "status".
/// </summary>
[AttributeUsage(AttributeTargets.Enum)]
public sealed class WireNameAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}

/// <summary>
/// Kisbetűs sztringként olvassa és írja az enumokat, és hibás értéknél olyan
/// üzenetet ad, amiből a kliens meg is érti, mi a baj:
///
///   Invalid bottle_type. Must be one of: pet, glass, aluminum, other
///
/// A beépített JsonStringEnumConverter ehelyett a .NET típusnevét szivárogtatná
/// ki ("could not be converted to Vissza.Shared.Enums.BottleType").
/// </summary>
public sealed class DomainEnumConverter : JsonConverterFactory
{
    /// <summary>A saját enumjaink névtere - másra nem nyúlunk.</summary>
    const string OwnNamespace = "Vissza.Shared.Enums";

    public override bool CanConvert(Type type) =>
        type.IsEnum && type.Namespace == OwnNamespace;

    public override JsonConverter CreateConverter(Type type, JsonSerializerOptions options) =>
        (JsonConverter)Activator.CreateInstance(
            typeof(DomainEnumConverter<>).MakeGenericType(type))!;
}

public sealed class DomainEnumConverter<TEnum> : JsonConverter<TEnum>
    where TEnum : struct, Enum
{
    static readonly string WireName =
        typeof(TEnum).GetCustomAttribute<WireNameAttribute>()?.Name
        ?? typeof(TEnum).Name.ToLowerInvariant();

    static readonly string AllowedValues =
        string.Join(", ", Enum.GetNames<TEnum>().Select(n => n.ToLowerInvariant()));

    public override TEnum Read(ref Utf8JsonReader reader, Type _, JsonSerializerOptions options)
    {
        var text = reader.TokenType == JsonTokenType.String ? reader.GetString() : null;

        if (text is not null && Enum.TryParse<TEnum>(text, ignoreCase: true, out var value)
            && Enum.IsDefined(value))
        {
            return value;
        }

        // A System.Text.Json ehhez hozzáfűzi a " Path: $.mezo | ..." részt;
        // azt a hibakezelő middleware vágja le a válasz előtt.
        throw new JsonException($"Invalid {WireName}. Must be one of: {AllowedValues}");
    }

    public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString().ToLowerInvariant());
}
