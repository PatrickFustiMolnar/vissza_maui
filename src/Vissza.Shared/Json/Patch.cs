using System.Text.Json;
using System.Text.Json.Serialization;

namespace Vissza.Shared.Json;

/// <summary>
/// Egy részleges frissítés egy mezője. Megkülönbözteti a három esetet, amit
/// egy sima nullable típus nem tud:
///
///   { }                       -> IsSet = false  (ne módosítsd)
///   { "notes": null }         -> IsSet = true,  Value = null  (töröld)
///   { "notes": "valami" }     -> IsSet = true,  Value = "valami"
///
/// Ez nem elméleti különbség. A régi backend PUT /offers/:id végpontja a
/// selected_collector_id-t közvetlenül írta felül, míg minden más mezőt
/// COALESCE-szal kezelt. Emiatt egy { "status": "completed" } kérés mellékesen
/// kinullázta a kiválasztott gyűjtőt, és az átvétel után a felajánló nem
/// tudott többé üzenni neki.
/// </summary>
[JsonConverter(typeof(PatchConverterFactory))]
public readonly struct Patch<T> : IEquatable<Patch<T>>
{
    public bool IsSet { get; }
    public T? Value { get; }

    Patch(T? value)
    {
        IsSet = true;
        Value = value;
    }

    /// <summary>A mező nem szerepelt a kérésben.</summary>
    public static Patch<T> Unset => default;

    public static Patch<T> Set(T? value) => new(value);

    /// <summary>
    /// Igaz, ha a mező szerepelt a kérésben; ilyenkor <paramref name="value"/>
    /// az új érték (ami lehet null is, ha törlést kértek).
    /// </summary>
    public bool TryGet(out T? value)
    {
        value = Value;
        return IsSet;
    }

    public bool Equals(Patch<T> other) =>
        IsSet == other.IsSet && EqualityComparer<T?>.Default.Equals(Value, other.Value);

    public override bool Equals(object? obj) => obj is Patch<T> other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(IsSet, Value);

    public static implicit operator Patch<T>(T? value) => Set(value);
}

public sealed class PatchConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type type) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Patch<>);

    public override JsonConverter CreateConverter(Type type, JsonSerializerOptions options)
    {
        var valueType = type.GetGenericArguments()[0];

        return (JsonConverter)Activator.CreateInstance(
            typeof(PatchConverter<>).MakeGenericType(valueType))!;
    }
}

file sealed class PatchConverter<T> : JsonConverter<Patch<T>>
{
    /// <summary>
    /// A System.Text.Json csak akkor hívja meg, ha a tulajdonság szerepel a
    /// JSON-ban - beleértve az explicit null-t is. Ezért elég a puszta hívás
    /// tényéből tudni, hogy a mezőt küldték.
    /// </summary>
    public override Patch<T> Read(ref Utf8JsonReader reader, Type _, JsonSerializerOptions options) =>
        Patch<T>.Set(JsonSerializer.Deserialize<T>(ref reader, options));

    public override void Write(Utf8JsonWriter writer, Patch<T> value, JsonSerializerOptions options)
    {
        // A be nem állított mezőket a JsonIgnoreCondition.WhenWritingDefault
        // hagyja ki teljesen; ide csak beállított érték jut el.
        JsonSerializer.Serialize(writer, value.Value, options);
    }
}
