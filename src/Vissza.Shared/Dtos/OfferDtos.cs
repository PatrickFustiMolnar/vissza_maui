using System.Text.Json.Serialization;
using Vissza.Shared.Enums;
using Vissza.Shared.Json;

namespace Vissza.Shared.Dtos;

/// <summary>
/// Egy felajánlás, a felajánló és a kiválasztott gyűjtő kilapított adataival.
///
/// A user mezők azért lapítottak, mert a régi API is így adta őket, és a
/// listaképernyők közvetlenül ezekre hivatkoznak (donor_name, donor_rating).
/// </summary>
public sealed record OfferDto
{
    public required int Id { get; init; }
    public required int DonorId { get; init; }
    public required int Quantity { get; init; }
    public required BottleType BottleType { get; init; }
    public string? OtherDescription { get; init; }

    /// <summary>Teljes URL, nem relatív útvonal.</summary>
    public string? PhotoUrl { get; init; }

    public required decimal LocationLat { get; init; }
    public required decimal LocationLng { get; init; }
    public required string Address { get; init; }
    public DateTime? AvailableFrom { get; init; }
    public DateTime? AvailableUntil { get; init; }
    public string? Notes { get; init; }
    public required OfferStatus Status { get; init; }
    public int? SelectedCollectorId { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }

    public string? DonorName { get; init; }
    public string? DonorProfileImage { get; init; }
    public decimal? DonorRating { get; init; }

    public string? SelectedCollectorName { get; init; }
    public string? SelectedCollectorProfileImage { get; init; }
    public decimal? SelectedCollectorRating { get; init; }
}

/// <summary>POST /api/offers</summary>
public sealed record CreateOfferRequest
{
    public int? Quantity { get; init; }
    public BottleType? BottleType { get; init; }
    public string? OtherDescription { get; init; }
    public string? PhotoUrl { get; init; }
    public decimal? LocationLat { get; init; }
    public decimal? LocationLng { get; init; }
    public string? Address { get; init; }
    public DateTime? AvailableFrom { get; init; }
    public DateTime? AvailableUntil { get; init; }
    public string? Notes { get; init; }

    /// <summary>Elhagyható; alapértelmezés <see cref="OfferStatus.Active"/>.</summary>
    public OfferStatus? Status { get; init; }
}

/// <summary>
/// PUT /api/offers/{id} - részleges frissítés.
///
/// Minden mező <see cref="Patch{T}"/>: a kihagyott mező változatlan marad, az
/// explicit null pedig töröl. Így a kliens továbbra is tud gyűjtőt levenni a
/// felajánlásról (selected_collector_id: null), anélkül hogy egy sima
/// státuszfrissítés véletlenül megtenné helyette.
/// </summary>
public sealed record UpdateOfferRequest
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Patch<int?> Quantity { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Patch<BottleType?> BottleType { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Patch<string?> OtherDescription { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Patch<string?> PhotoUrl { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Patch<decimal?> LocationLat { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Patch<decimal?> LocationLng { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Patch<string?> Address { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Patch<DateTime?> AvailableFrom { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Patch<DateTime?> AvailableUntil { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Patch<string?> Notes { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Patch<OfferStatus?> Status { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Patch<int?> SelectedCollectorId { get; init; }
}
