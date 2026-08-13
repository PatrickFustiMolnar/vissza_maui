using Vissza.Shared.Enums;

namespace Vissza.Api.Entities;

/// <summary>offers tábla</summary>
public class Offer
{
    public int Id { get; set; }
    public int DonorId { get; set; }
    public int Quantity { get; set; }
    public BottleType BottleType { get; set; }

    /// <summary>Csak akkor van kitöltve, ha a BottleType Other.</summary>
    public string? OtherDescription { get; set; }

    public string? PhotoUrl { get; set; }
    public decimal LocationLat { get; set; }
    public decimal LocationLng { get; set; }
    public string Address { get; set; } = null!;
    public DateTime? AvailableFrom { get; set; }
    public DateTime? AvailableUntil { get; set; }
    public string? Notes { get; set; }

    public OfferStatus Status { get; set; } = OfferStatus.Active;

    /// <summary>
    /// A kiválasztott gyűjtő. A séma ON DELETE SET NULL-t használ, ezért
    /// nullázható: ha a gyűjtő fiókja törlődik, a felajánlás megmarad.
    /// </summary>
    public int? SelectedCollectorId { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User Donor { get; set; } = null!;
    public User? SelectedCollector { get; set; }
    public ICollection<CollectionRequest> CollectionRequests { get; set; } = new List<CollectionRequest>();
}
