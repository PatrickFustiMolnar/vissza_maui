using Vissza.Shared.Enums;

namespace Vissza.Maui.Resources;

/// <summary>
/// Palacktípus- és állapotcímkék, a régi constants/bottleTypes.js-ből.
///
/// A régi kódban ez a tábla három képernyőn volt szó szerint lemásolva, így
/// egy címke átírása kettőt érintetlenül hagyott volna. Egy helyen van.
/// </summary>
public static class DomainLabels
{
    public static string BottleType(BottleType type) => type switch
    {
        Shared.Enums.BottleType.Pet => "PET palack",
        Shared.Enums.BottleType.Glass => "Üvegpalack",
        Shared.Enums.BottleType.Aluminum => "Alumínium",
        _ => "Egyéb"
    };

    /// <summary>Rövid változat szűk helyre. Szándékosan tér el a hosszútól.</summary>
    public static string BottleTypeShort(BottleType type) => type switch
    {
        Shared.Enums.BottleType.Pet => "PET",
        Shared.Enums.BottleType.Glass => "Üveg",
        Shared.Enums.BottleType.Aluminum => "Alumínium",
        _ => "Egyéb"
    };

    public static string UserRole(UserRole role) => role switch
    {
        Shared.Enums.UserRole.Donor => "Csak felajánló",
        Shared.Enums.UserRole.Collector => "Csak gyűjtő",
        _ => "Mindkettő"
    };

    public static string OfferStatus(OfferStatus status) => status switch
    {
        Shared.Enums.OfferStatus.Active => "Aktív",
        Shared.Enums.OfferStatus.Reserved => "Folyamatban",
        _ => "Lezárt"
    };

    /// <summary>
    /// A jelvény színkulcsa. A BadgeView ezt fordítja színpárra - a lezárt és
    /// a visszavont ugyanazt a semleges színt kapja.
    /// </summary>
    public static string StatusBadgeKind(OfferStatus status) => status switch
    {
        Shared.Enums.OfferStatus.Active => "active",
        Shared.Enums.OfferStatus.Reserved => "reserved",
        _ => "neutral"
    };

    public static string BottleTypeBadgeKind(BottleType type) => type.ToString().ToLowerInvariant();

    /// <summary>
    /// A visszaváltási érték becslése: 50 Ft palackonként, ahogy a régi
    /// appban. Nem konfigurálható, mert a valódi betétdíj is fix.
    /// </summary>
    public static int EstimatedValue(int quantity) => quantity * 50;
}
