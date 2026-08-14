namespace Vissza.Maui.Maps;

/// <summary>Mit jelöl egy tű a térképen. A színét is ez dönti el.</summary>
public enum MapPinKind
{
    /// <summary>A felhasználó saját helyzete.</summary>
    User,

    /// <summary>Palack-felajánlás.</summary>
    Offer,

    /// <summary>Visszaváltó hely (automata, üzlet, gyűjtőpont).</summary>
    ReturnLocation
}

/// <summary>
/// Egy megjelenítendő pont a térképen, WGS84 koordinátákkal.
///
/// A vetítés a <see cref="VisszaMapView"/> dolga - itt szándékosan
/// szélesség/hosszúság van, ahogy az adatbázisban is.
/// </summary>
public sealed record MapPin
{
    public required MapPinKind Kind { get; init; }
    public required double Latitude { get; init; }
    public required double Longitude { get; init; }
    public required string Title { get; init; }
    public string? Subtitle { get; init; }

    /// <summary>A koppintás visszaadja, hogy a hívó tudja, mire kattintottak.</summary>
    public object? Payload { get; init; }
}
