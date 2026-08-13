using Vissza.Shared.Enums;

namespace Vissza.Api.Entities;

/// <summary>
/// return_locations tábla - visszaváltó automaták, üzletek, gyűjtőpontok.
///
/// Az egyetlen tábla, ami nem kötődik felhasználóhoz, és a hozzá tartozó
/// két végpont az egyetlen, ami token nélkül is hívható.
/// </summary>
public class ReturnLocation
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Address { get; set; } = null!;
    public decimal Lat { get; set; }
    public decimal Lng { get; set; }
    public LocationType Type { get; set; }

    /// <summary>Szabad szöveg vagy JSON - a séma nem köti meg.</summary>
    public string? OpeningHours { get; set; }

    /// <summary>Vesszővel elválasztott palacktípusok, pl. "pet,glass".</summary>
    public string? AcceptedTypes { get; set; }

    public string? Contact { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
