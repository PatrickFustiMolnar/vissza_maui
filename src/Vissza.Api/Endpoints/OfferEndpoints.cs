using System.Security.Claims;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vissza.Api.Data;
using Vissza.Api.Entities;
using Vissza.Api.Mapping;
using Vissza.Api.Services;
using Vissza.Shared.Dtos;
using Vissza.Shared.Enums;
using Vissza.Shared.Json;

namespace Vissza.Api.Endpoints;

public static class OfferEndpoints
{
    public static void MapOfferEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/offers").RequireAuthorization();

        group.MapGet("/", ListAsync);
        group.MapGet("/{id:int}", GetAsync);
        group.MapPost("/", CreateAsync);
        group.MapPut("/{id:int}", UpdateAsync);
        group.MapDelete("/{id:int}", DeleteAsync);
    }

    static async Task<IResult> ListAsync(
        VisszaDbContext db,
        ImageUrlService imageUrls,
        CancellationToken ct,
        // A kliens snake_case query paramétereket küld (donor_id), a
        // névpolitika viszont csak a JSON törzsre vonatkozik - ezért kell
        // a kötést itt kézzel megadni.
        [FromQuery(Name = "status")] string? status = null,
        [FromQuery(Name = "donor_id")] int? donorId = null,
        [FromQuery(Name = "bottle_type")] string? bottleType = null,
        [FromQuery(Name = "min_quantity")] int? minQuantity = null)
    {
        var query = db.Offers.AsNoTracking();

        if (!string.IsNullOrEmpty(status))
        {
            if (!TryParseEnum<OfferStatus>(status, out var parsed, out var error))
                return Results.BadRequest(error);

            query = query.Where(o => o.Status == parsed);
        }

        if (donorId is not null)
            query = query.Where(o => o.DonorId == donorId);

        if (!string.IsNullOrEmpty(bottleType))
        {
            if (!TryParseEnum<BottleType>(bottleType, out var parsed, out var error))
                return Results.BadRequest(error);

            query = query.Where(o => o.BottleType == parsed);
        }

        // A kliens csak pozitív értéket küld; a 0 és a negatív "nincs szűrés".
        if (minQuantity is > 0)
            query = query.Where(o => o.Quantity >= minQuantity);

        var offers = await query
            .OrderByDescending(o => o.CreatedAt)
            .ProjectToDto()
            .ToListAsync(ct);

        return Results.Ok(offers.Select(o => o.ToAbsoluteUrls(imageUrls)));
    }

    static async Task<IResult> GetAsync(
        int id,
        VisszaDbContext db,
        ImageUrlService imageUrls,
        CancellationToken ct)
    {
        var offer = await db.Offers
            .AsNoTracking()
            .Where(o => o.Id == id)
            .ProjectToDto()
            .FirstOrDefaultAsync(ct);

        return offer is null
            ? Results.NotFound(new MessageResponse("Offer not found"))
            : Results.Ok(offer.ToAbsoluteUrls(imageUrls));
    }

    static async Task<IResult> CreateAsync(
        CreateOfferRequest request,
        ClaimsPrincipal principal,
        VisszaDbContext db,
        ImageUrlService imageUrls,
        CancellationToken ct)
    {
        if (request.Quantity is not > 0
            || request.BottleType is null
            || string.IsNullOrWhiteSpace(request.Address)
            || request.LocationLat is null
            || request.LocationLng is null)
        {
            return Results.BadRequest(new MessageResponse("Missing required fields"));
        }

        var offer = new Offer
        {
            DonorId = principal.GetUserId(),
            Quantity = request.Quantity.Value,
            BottleType = request.BottleType.Value,
            OtherDescription = request.OtherDescription,
            PhotoUrl = request.PhotoUrl,
            LocationLat = request.LocationLat.Value,
            LocationLng = request.LocationLng.Value,
            Address = request.Address,
            AvailableFrom = request.AvailableFrom,
            AvailableUntil = request.AvailableUntil,
            Notes = request.Notes,
            Status = request.Status ?? OfferStatus.Active
        };

        db.Offers.Add(offer);
        await db.SaveChangesAsync(ct);

        var created = await LoadDtoAsync(db, offer.Id, imageUrls, ct);

        return Results.Created($"/api/offers/{offer.Id}", created);
    }

    static async Task<IResult> UpdateAsync(
        int id,
        UpdateOfferRequest request,
        ClaimsPrincipal principal,
        VisszaDbContext db,
        ImageUrlService imageUrls,
        CancellationToken ct)
    {
        var offer = await db.Offers.FirstOrDefaultAsync(o => o.Id == id, ct);

        if (offer is null)
            return Results.NotFound(new MessageResponse("Offer not found"));

        if (offer.DonorId != principal.GetUserId())
            return Results.Json(new MessageResponse("Not authorized"), statusCode: 403);

        // Csak a ténylegesen elküldött mezők módosulnak. A selected_collector_id
        // is így viselkedik: kihagyva változatlan marad, explicit null-lal
        // törölhető. A régi backend ezt az egy mezőt mindig felülírta, ezért
        // egy sima státuszfrissítés is levette a gyűjtőt a felajánlásról.
        if (request.Quantity.TryGet(out var quantity) && quantity is not null)
            offer.Quantity = quantity.Value;

        if (request.BottleType.TryGet(out var bottleType) && bottleType is not null)
            offer.BottleType = bottleType.Value;

        if (request.OtherDescription.TryGet(out var otherDescription))
            offer.OtherDescription = otherDescription;

        if (request.PhotoUrl.TryGet(out var photoUrl))
            offer.PhotoUrl = photoUrl;

        if (request.LocationLat.TryGet(out var lat) && lat is not null)
            offer.LocationLat = lat.Value;

        if (request.LocationLng.TryGet(out var lng) && lng is not null)
            offer.LocationLng = lng.Value;

        if (request.Address.TryGet(out var address) && address is not null)
            offer.Address = address;

        if (request.AvailableFrom.TryGet(out var availableFrom))
            offer.AvailableFrom = availableFrom;

        if (request.AvailableUntil.TryGet(out var availableUntil))
            offer.AvailableUntil = availableUntil;

        if (request.Notes.TryGet(out var notes))
            offer.Notes = notes;

        if (request.Status.TryGet(out var status) && status is not null)
            offer.Status = status.Value;

        if (request.SelectedCollectorId.TryGet(out var collectorId))
            offer.SelectedCollectorId = collectorId;

        await db.SaveChangesAsync(ct);

        return Results.Ok(await LoadDtoAsync(db, offer.Id, imageUrls, ct));
    }

    static async Task<IResult> DeleteAsync(
        int id,
        ClaimsPrincipal principal,
        VisszaDbContext db,
        CancellationToken ct)
    {
        var offer = await db.Offers.FirstOrDefaultAsync(o => o.Id == id, ct);

        if (offer is null)
            return Results.NotFound(new MessageResponse("Offer not found"));

        if (offer.DonorId != principal.GetUserId())
            return Results.Json(new MessageResponse("Not authorized"), statusCode: 403);

        db.Offers.Remove(offer);
        await db.SaveChangesAsync(ct);

        return Results.Ok(new MessageResponse("Offer deleted successfully"));
    }

    /// <summary>
    /// Enum query paraméter feldolgozása. Kis-nagybetű független, mert a
    /// query paraméterek kötése nem megy át a JSON konverteren - a hibaüzenet
    /// viszont ugyanaz, mint amit a törzsben lévő rossz érték adna.
    /// </summary>
    static bool TryParseEnum<TEnum>(string text, out TEnum value, out MessageResponse error)
        where TEnum : struct, Enum
    {
        if (Enum.TryParse(text, ignoreCase: true, out value) && Enum.IsDefined(value))
        {
            error = null!;
            return true;
        }

        var wireName = typeof(TEnum).GetCustomAttribute<WireNameAttribute>()?.Name
            ?? typeof(TEnum).Name.ToLowerInvariant();

        var allowed = string.Join(", ", Enum.GetNames<TEnum>().Select(n => n.ToLowerInvariant()));

        error = new MessageResponse($"Invalid {wireName}. Must be one of: {allowed}");
        return false;
    }

    /// <summary>
    /// Írás után újraolvassa a sort a vetítéssel, hogy a válasz ugyanúgy
    /// nézzen ki, mint a listáké - és hogy az adatbázis által generált
    /// created_at / updated_at is a valódi értéket mutassa.
    /// </summary>
    static async Task<OfferDto> LoadDtoAsync(
        VisszaDbContext db,
        int id,
        ImageUrlService imageUrls,
        CancellationToken ct)
    {
        var dto = await db.Offers
            .AsNoTracking()
            .Where(o => o.Id == id)
            .ProjectToDto()
            .FirstAsync(ct);

        return dto.ToAbsoluteUrls(imageUrls);
    }
}
