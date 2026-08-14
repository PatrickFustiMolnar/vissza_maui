namespace Vissza.Maui.Services;

/// <summary>
/// Az API elérhetősége. Nem csak a HTTP-kliensnek kell: a feltöltés relatív
/// útvonalat ad vissza (/uploads/kep.jpg), és amíg az a szerverre vissza nem
/// kerül, a kliensnek magának kell teljes URL-t csinálnia belőle az
/// előnézethez.
/// </summary>
public static class ApiConfig
{
    /// <summary>
    /// Fejlesztéskor a helyi gép. Az Android emulátor saját hálózaton fut,
    /// onnan a gazdagép a 10.0.2.2 címen érhető el - az iOS szimulátor
    /// viszont a gazdagép hálózatát használja.
    ///
    /// Élesben ide az api.fustimolnarpatrick.com kerül majd, HTTPS-en.
    /// </summary>
    public static string BaseUrl => DeviceInfo.Platform == DevicePlatform.Android
        ? "http://10.0.2.2:5199"
        : "http://localhost:5199";

    /// <summary>
    /// Relatív képútvonalból teljes URL. A már teljes URL-t változatlanul
    /// hagyja: a régi adatokban vegyesen fordul elő mindkét alak, ahogy a
    /// szerveroldali ImageUrlService is számol vele.
    /// </summary>
    public static string? Absolute(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        return path.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                ? path
                : $"{BaseUrl}{path}";
    }
}
