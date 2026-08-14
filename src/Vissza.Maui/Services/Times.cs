namespace Vissza.Maui.Services;

/// <summary>
/// Időzóna a kliens és az API között.
///
/// A szerződés: **a dróton és az adatbázisban UTC van, a felületen helyi idő.**
/// A MySQL DATETIME nem tárol zónát, az adatbázis-kiszolgáló pedig UTC-ben jár
/// (a CURRENT_TIMESTAMP is azt írja), tehát a tárolt érték UTC. A JSON-ben
/// viszont jelöletlenül jön vissza (nincs "Z"), így a .NET
/// DateTimeKind.Unspecified-ként kapja - ha nyersen írnánk ki, nyáron két
/// órával korábbi időt mutatnánk.
/// </summary>
public static class Times
{
    /// <summary>Az API-tól kapott, jelöletlen UTC érték helyi időre.</summary>
    public static DateTime ToLocal(DateTime value) =>
        DateTime.SpecifyKind(value, DateTimeKind.Utc).ToLocalTime();

    /// <summary>A felhasználó helyi választása UTC-re, ahogy a szerver tárolja.</summary>
    public static DateTime ToServer(DateTime local) =>
        DateTime.SpecifyKind(local, DateTimeKind.Local).ToUniversalTime();
}
