using Vissza.Api.Entities;
using Vissza.Api.Services;
using Vissza.Shared.Dtos;

namespace Vissza.Api.Mapping;

public static class OfferMapping
{
    /// <summary>
    /// Egyetlen SQL lekérdezés, LEFT JOIN-okkal - nincs N+1.
    ///
    /// Szándékosan vetítés, nem Include: így csak a listákhoz kellő négy
    /// felhasználói oszlop jön át. Az e-mail és a password_hash bele sem
    /// kerül a lekérdezésbe, tehát ki sem szivároghat.
    /// </summary>
    public static IQueryable<OfferDto> ProjectToDto(this IQueryable<Offer> offers) =>
        offers.Select(o => new OfferDto
        {
            Id = o.Id,
            DonorId = o.DonorId,
            Quantity = o.Quantity,
            BottleType = o.BottleType,
            OtherDescription = o.OtherDescription,

            // Relatív útvonal - a ToAbsoluteUrls teszi teljessé, mert a
            // kérés hosztja nem áll rendelkezésre az SQL-ben.
            PhotoUrl = o.PhotoUrl,

            LocationLat = o.LocationLat,
            LocationLng = o.LocationLng,
            Address = o.Address,
            AvailableFrom = o.AvailableFrom,
            AvailableUntil = o.AvailableUntil,
            Notes = o.Notes,
            Status = o.Status,
            SelectedCollectorId = o.SelectedCollectorId,
            CreatedAt = o.CreatedAt,
            UpdatedAt = o.UpdatedAt,

            DonorName = o.Donor.Name,
            DonorProfileImage = o.Donor.ProfileImage,
            DonorRating = o.Donor.AverageRating,

            SelectedCollectorName = o.SelectedCollector != null ? o.SelectedCollector.Name : null,
            SelectedCollectorProfileImage = o.SelectedCollector != null ? o.SelectedCollector.ProfileImage : null,
            SelectedCollectorRating = o.SelectedCollector != null ? o.SelectedCollector.AverageRating : null
        });

    public static OfferDto ToAbsoluteUrls(this OfferDto dto, ImageUrlService imageUrls) => dto with
    {
        PhotoUrl = imageUrls.ToAbsolute(dto.PhotoUrl),
        DonorProfileImage = imageUrls.ToAbsolute(dto.DonorProfileImage),
        SelectedCollectorProfileImage = imageUrls.ToAbsolute(dto.SelectedCollectorProfileImage)
    };
}
