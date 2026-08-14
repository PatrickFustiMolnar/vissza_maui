namespace Vissza.Maui.Maps;

/// <summary>
/// Távolság két koordináta között.
///
/// Kliensoldalon számoljuk, mert a szerver nem ismeri a felhasználó
/// helyzetét - az a készüléken van, és nem is akarjuk elküldeni.
/// </summary>
public static class GeoDistance
{
    /// <summary>A Föld sugara méterben, ahogy a WGS84 használja.</summary>
    const double EarthRadiusMeters = 6378137.0;

    /// <summary>Haversine-távolság méterben.</summary>
    public static double Meters(double lat1, double lng1, double lat2, double lng2)
    {
        var dLat = ToRadians(lat2 - lat1);
        var dLng = ToRadians(lng2 - lng1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                + Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2))
                  * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);

        return EarthRadiusMeters * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    public static double Kilometers(double lat1, double lng1, double lat2, double lng2) =>
        Meters(lat1, lng1, lat2, lng2) / 1000.0;

    static double ToRadians(double degrees) => degrees * Math.PI / 180.0;
}
