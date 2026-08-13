using System.Text.Json;
using Vissza.Shared.Json;

namespace Vissza.Maui.Services;

/// <summary>
/// A JSON beállítások egyetlen forrása a kliensen.
///
/// Pontosan meg kell egyeznie a szerver Program.cs-ében beállítottal:
/// snake_case tulajdonságnevek és a saját enum konverter. Ha a kettő
/// szétcsúszik, a hiba néma - a mezők egyszerűen null-ok lesznek.
/// </summary>
public static class ApiJson
{
    public static JsonSerializerOptions Options { get; } = Create();

    static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = true
        };

        options.Converters.Add(new DomainEnumConverter());

        return options;
    }
}
