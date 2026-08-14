using System.Text.Json;
using Refit;
using Vissza.Shared.Dtos;

namespace Vissza.Maui.Services;

public static class ApiErrors
{
    /// <summary>
    /// Az API minden hibát <c>{ "message": "..." }</c> alakban ad vissza.
    /// Ez húzza ki belőle az üzenetet, hogy a felhasználó azt lássa, amit a
    /// szerver mondott, ne egy nyers státuszkódot.
    /// </summary>
    public static string Describe(Exception exception) => exception switch
    {
        ApiException api => FromApi(api),

        // A saját üzenete már magyar, és a felhasználónak szól.
        PhotoPickException => exception.Message,

        HttpRequestException => "Nem sikerült elérni a szervert. Ellenőrizd a hálózati kapcsolatot.",
        TaskCanceledException => "A kérés túllépte az időkorlátot.",

        // Fejlesztői módban a kivétel típusa és üzenete is látszik. Enélkül
        // minden ismeretlen hiba ugyanarra az egy mondatra fut, és a
        // hibakeresés a naplóban sem talál semmit, ha a kérés el sem indult.
#if DEBUG
        _ => $"{exception.GetType().Name}: {exception.Message}"
#else
        _ => "Váratlan hiba történt."
#endif
    };

    static string FromApi(ApiException exception)
    {
        if (!string.IsNullOrEmpty(exception.Content))
        {
            try
            {
                var response = JsonSerializer.Deserialize<MessageResponse>(
                    exception.Content, ApiJson.Options);

                if (!string.IsNullOrWhiteSpace(response?.Message))
                    return response.Message;
            }
            catch (JsonException)
            {
                // Nem a megszokott hibaalak - lentebb az általános üzenet megy.
            }
        }

        return (int)exception.StatusCode switch
        {
            401 or 403 => "A művelethez be kell jelentkezned.",
            404 => "A keresett elem nem található.",
            429 => "Túl sok próbálkozás. Várj egy kicsit.",
            _ => "A szerver hibát jelzett."
        };
    }
}
