using Vissza.Shared.Enums;

namespace Vissza.Api.Entities;

/// <summary>
/// transactions tábla - az átvétel maga.
///
/// A státusz csak akkor lehet Completed, ha DonorConfirmed és
/// CollectorConfirmed is igaz. Ezt szerveroldalon kell kikényszeríteni:
/// kliensoldali ellenőrzésként egy módosított kliens megkerülné.
/// </summary>
public class Transaction
{
    public int Id { get; set; }
    public int OfferId { get; set; }
    public int DonorId { get; set; }
    public int CollectorId { get; set; }

    public DateTime? PickupDate { get; set; }
    public string? Location { get; set; }

    /// <summary>
    /// A felajánlás adatainak másolata az átvétel pillanatában. Szándékos
    /// duplikáció: ha a felajánlás később módosul, a tranzakció attól még
    /// azt őrzi, amiben a felek megállapodtak.
    /// </summary>
    public int Quantity { get; set; }

    public BottleType BottleType { get; set; }

    public bool DonorConfirmed { get; set; }
    public bool CollectorConfirmed { get; set; }
    public TransactionStatus Status { get; set; } = TransactionStatus.Pending;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Offer Offer { get; set; } = null!;
    public User Donor { get; set; } = null!;
    public User Collector { get; set; } = null!;
    public ICollection<Rating> Ratings { get; set; } = new List<Rating>();
}
