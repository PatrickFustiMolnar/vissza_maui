using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Vissza.Maui.Services;

/// <summary>
/// Cím → koordináta feloldás az OpenStreetMap Nominatim szolgáltatásával.
/// Kulcs nélküli, ahogy a térkép csempéi is.
///
/// A régi appban ez a függvény két képernyőn volt szó szerint lemásolva
/// (GiveScreen és SettingsScreen), itt egy helyen van.
///
/// A Nominatim használati szabályzata megköveteli az azonosítható
/// User-Agent fejlécet, és másodpercenként legfeljebb egy kérést enged -
/// a felhasználó kézi címbeírásánál ez nem korlát.
/// </summary>
public sealed class GeocodingService
{
    const string Endpoint = "https://nominatim.openstreetmap.org/search";

    readonly HttpClient _http = new()
    {
        Timeout = TimeSpan.FromSeconds(15),
        DefaultRequestHeaders = { { "User-Agent", "Vissza/1.0 (hu.fustimolnarpatrick.vissza)" } }
    };

    /// <summary>
    /// Visszaadja a cím koordinátáit, vagy null-t, ha nem található.
    /// A hívó dolga eldönteni, mit kezd a null-lal - itt nincs kivétel,
    /// mert a "nem találtam" nem hiba, hanem eredmény.
    /// </summary>
    public async Task<(double Lat, double Lng)?> ResolveAsync(string address, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(address))
            return null;

        try
        {
            var url = $"{Endpoint}?format=json&limit=1&q={Uri.EscapeDataString(address)}";
            var results = await _http.GetFromJsonAsync<NominatimResult[]>(url, ct);

            if (results is not [{ } first, ..])
                return null;

            return double.TryParse(first.Lat, System.Globalization.CultureInfo.InvariantCulture, out var lat)
                && double.TryParse(first.Lon, System.Globalization.CultureInfo.InvariantCulture, out var lng)
                    ? (lat, lng)
                    : null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Geokódolás nem sikerült: {ex.Message}");
            return null;
        }
    }

    /// <summary>A Nominatim sztringként adja vissza a koordinátákat.</summary>
    sealed record NominatimResult(
        [property: JsonPropertyName("lat")] string Lat,
        [property: JsonPropertyName("lon")] string Lon);
}
