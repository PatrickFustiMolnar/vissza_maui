using System.Linq.Expressions;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vissza.Api.Data;
using Vissza.Api.Entities;
using Vissza.Api.Services;
using Vissza.Shared.Dtos;
using Vissza.Shared.Enums;

namespace Vissza.Api.Endpoints;

public static class CollectionRequestEndpoints
{
    public static void MapCollectionRequestEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/collection-requests").RequireAuthorization();

        group.MapGet("/", ListAsync);
        group.MapPost("/", CreateAsync);
        group.MapPut("/{id:int}", UpdateAsync);
    }

    static async Task<IResult> ListAsync(
        ClaimsPrincipal principal,
        VisszaDbContext db,
        ImageUrlService imageUrls,
        CancellationToken ct,
        [FromQuery(Name = "offer_id")] int? offerId = null,
        [FromQuery(Name = "collector_id")] int? collectorId = null,
        [FromQuery(Name = "status")] string? status = null)
    {
        var userId = principal.GetUserId();

        // Láthatóság: csak a saját jelentkezéseid, vagy a saját
        // felajánlásaidra érkezettek. Nem opcionális, és a hívó szűrői
        // előtt érvényesül.
        var query = db.CollectionRequests
            .AsNoTracking()
            .Where(r => r.CollectorId == userId || r.Offer.DonorId == userId);

        if (offerId is not null)
            query = query.Where(r => r.OfferId == offerId);

        if (collectorId is not null)
            query = query.Where(r => r.CollectorId == collectorId);

        if (!string.IsNullOrEmpty(status))
        {
            if (!EnumQuery.TryParse<RequestStatus>(status, out var parsed, out var error))
                return Results.BadRequest(error);

            query = query.Where(r => r.Status == parsed);
        }

        var requests = await query
            .OrderByDescending(r => r.CreatedAt)
            .Select(ToDto)
            .ToListAsync(ct);

        return Results.Ok(requests.Select(r => r with
        {
            CollectorProfileImage = imageUrls.ToAbsolute(r.CollectorProfileImage)
        }));
    }

    static async Task<IResult> CreateAsync(
        CreateCollectionRequestRequest request,
        ClaimsPrincipal principal,
        VisszaDbContext db,
        CancellationToken ct)
    {
        if (request.OfferId is null)
            return Results.BadRequest(new MessageResponse("offer_id is required"));

        var userId = principal.GetUserId();

        if (!await db.Offers.AnyAsync(o => o.Id == request.OfferId, ct))
            return Results.NotFound(new MessageResponse("Offer not found"));

        var duplicate = await db.CollectionRequests
            .AnyAsync(r => r.OfferId == request.OfferId && r.CollectorId == userId, ct);

        if (duplicate)
            return Results.BadRequest(new MessageResponse("Request already exists"));

        var entity = new CollectionRequest
        {
            OfferId = request.OfferId.Value,
            CollectorId = userId,
            Status = request.Status ?? RequestStatus.Pending,
            Message = request.Message
        };

        db.CollectionRequests.Add(entity);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/collection-requests/{entity.Id}",
            await LoadDtoAsync(db, entity.Id, ct));
    }

    /// <summary>
    /// Az elfogadás négy írás egyetlen logikai lépésben: a kérés elfogadása,
    /// a felajánlás lefoglalása, a rivális kérések elutasítása és az átvétel
    /// létrehozása. Ha bármelyik elhal, félkész állapot maradna - elfogadott
    /// kérés lefoglalás nélkül, vagy lefoglalt felajánlás átvétel nélkül.
    /// Ezért fut mind egyetlen adatbázis-tranzakcióban.
    /// </summary>
    static async Task<IResult> UpdateAsync(
        int id,
        UpdateCollectionRequestRequest request,
        ClaimsPrincipal principal,
        VisszaDbContext db,
        CancellationToken ct)
    {
        var userId = principal.GetUserId();
        request.Status.TryGet(out var requestedStatus);

        await using var dbTransaction = await db.Database.BeginTransactionAsync(ct);

        // Zárolatlan előolvasás, csak az offer_id-ért: a zárolási sorrendet
        // (előbb felajánlás, utána kérés) ez teszi betarthatóvá.
        var offerId = await db.CollectionRequests
            .AsNoTracking()
            .Where(r => r.Id == id)
            .Select(r => (int?)r.OfferId)
            .FirstOrDefaultAsync(ct);

        if (offerId is null)
            return Results.NotFound(new MessageResponse("Request not found"));

        // A felajánlás sorzárja dönti el, ki foglalhatja le: e nélkül két
        // párhuzamosan elfogadott kérés egymásra írná a selected_collector_id-t.
        var offer = await db.LockOfferAsync(offerId.Value, ct);

        if (offer is null)
            return Results.NotFound(new MessageResponse("Offer not found"));

        var entity = await db.LockCollectionRequestAsync(id, ct);

        if (entity is null)
            return Results.NotFound(new MessageResponse("Request not found"));

        var isDonor = offer.DonorId == userId;
        var isCollector = entity.CollectorId == userId;

        if (!isDonor && !isCollector)
            return Results.Json(new MessageResponse("Not authorized"), statusCode: 403);

        // Az elfogadás és az elutasítás kizárólag a felajánló döntése. E nélkül
        // egy gyűjtő elfogadhatná a saját kérését, magának foglalhatná a
        // felajánlást, és automatikusan elutasíthatna minden riválist.
        var decides = requestedStatus is RequestStatus.Accepted or RequestStatus.Rejected;

        if (decides && !isDonor)
            return Results.Json(new MessageResponse("Not authorized"), statusCode: 403);

        // Egy felajánlást egyszerre egy gyűjtő vihet el. Másik jelentkezőre
        // váltani a lefoglalás visszavonása után lehet - az visszaállítja a
        // felajánlást aktívra.
        if (requestedStatus is RequestStatus.Accepted
            && offer.Status != OfferStatus.Active
            && offer.SelectedCollectorId != entity.CollectorId)
        {
            return Results.Json(
                new MessageResponse("This offer is no longer available"), statusCode: 409);
        }

        if (request.Message.TryGet(out var message) && message is not null)
            entity.Message = message;

        // A státusz feltételes írása mondja meg, hogy tényleg most történt-e az
        // átmenet. Csak ekkor szabad a mellékhatásokat lefuttatni; egy ismételt
        // kérés így nem foglalja le újra a felajánlást.
        var transitioned = requestedStatus is not null && entity.Status != requestedStatus;

        if (transitioned)
            entity.Status = requestedStatus!.Value;

        await db.SaveChangesAsync(ct);

        if (requestedStatus is RequestStatus.Accepted && transitioned)
            await ApplyAcceptanceAsync(db, offer, entity, ct);

        await dbTransaction.CommitAsync(ct);

        return Results.Ok(await LoadDtoAsync(db, id, ct));
    }

    static async Task ApplyAcceptanceAsync(
        VisszaDbContext db, Offer offer, CollectionRequest accepted, CancellationToken ct)
    {
        offer.SelectedCollectorId = accepted.CollectorId;
        offer.Status = OfferStatus.Reserved;

        await db.CollectionRequests
            .Where(r => r.OfferId == offer.Id
                        && r.Id != accepted.Id
                        && r.Status == RequestStatus.Pending)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.Status, RequestStatus.Rejected), ct);

        // Az átvétel létrehozása nem "best effort": ha nem jön létre, a
        // lefoglalás sem érvényes, és az egész tranzakció visszagördül.
        var hasOpenTransaction = await db.Transactions
            .AnyAsync(t => t.OfferId == offer.Id && t.Status != TransactionStatus.Cancelled, ct);

        if (!hasOpenTransaction)
        {
            db.Transactions.Add(new Transaction
            {
                OfferId = offer.Id,
                DonorId = offer.DonorId,
                CollectorId = accepted.CollectorId,
                Location = offer.Address,
                Quantity = offer.Quantity,
                BottleType = offer.BottleType,
                Status = TransactionStatus.Pending
            });
        }

        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Kifejezésfaként, nem metódusként: így az EF le tudja fordítani SQL-re,
    /// és a gyűjtő adatai egyetlen JOIN-nal jönnek, nem elemenkénti körrel.
    /// </summary>
    static readonly Expression<Func<CollectionRequest, CollectionRequestDto>> ToDto =
        r => new CollectionRequestDto
        {
            Id = r.Id,
            OfferId = r.OfferId,
            CollectorId = r.CollectorId,
            Status = r.Status,
            Message = r.Message,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt,
            CollectorName = r.Collector.Name,
            CollectorProfileImage = r.Collector.ProfileImage,
            CollectorRating = r.Collector.AverageRating
        };

    static async Task<CollectionRequestDto> LoadDtoAsync(
        VisszaDbContext db, int id, CancellationToken ct) =>
        await db.CollectionRequests
            .AsNoTracking()
            .Where(r => r.Id == id)
            .Select(ToDto)
            .FirstAsync(ct);
}
