namespace Vissza.Api.Services;

/// <summary>
/// A képek relatív útvonala az adatbázisban van (/uploads/kep.jpg), a kliens
/// viszont teljes URL-t vár. A régi backend getImageUrl-jének megfelelője.
/// </summary>
public sealed class ImageUrlService(IHttpContextAccessor accessor)
{
    public string? ToAbsolute(string? path)
    {
        if (string.IsNullOrEmpty(path))
            return null;

        // Már teljes URL: a régi adatokban vegyesen fordul elő mindkét alak.
        if (path.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return path;
        }

        if (!path.StartsWith("/uploads/", StringComparison.Ordinal))
            return path;

        var request = accessor.HttpContext?.Request;

        // Kérés-kontextus nélkül (pl. háttérfeladatból) nincs mihez képest
        // abszolutizálni; ilyenkor a relatív útvonal a helyes válasz.
        return request is null
            ? path
            : $"{request.Scheme}://{request.Host}{path}";
    }
}
