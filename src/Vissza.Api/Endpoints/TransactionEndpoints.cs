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

public static class TransactionEndpoints
{
    public static void MapTransactionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/transactions").RequireAuthorization();

        group.MapGet("/", ListAsync);
        group.MapGet("/{id:int}", GetAsync);
        group.MapPost("/", CreateAsync);
        group.MapPut("/{id:int}", UpdateAsync);
    }

    static async Task<IResult> ListAsync(
        ClaimsPrincipal principal,
        VisszaDbContext db,
        CancellationToken ct,
        [FromQuery(Name = "offer_id")] int? offerId = null,
        [FromQuery(Name = "donor_id")] int? donorId = null,
        [FromQuery(Name = "collector_id")] int? collectorId = null,
        [FromQuery(Name = "status")] string? status = null)
    {
        var userId = principal.GetUserId();

        // Az átvétel a két fél magánügye. Ez a szűkítés nem opcionális, és a
        // hívó szűrői előtt érvényesül.
        var query = db.Transactions
            .AsNoTracking()
            .Where(t => t.DonorId == userId || t.CollectorId == userId);

        if (offerId is not null)
            query = query.Where(t => t.OfferId == offerId);

        if (donorId is not null)
            query = query.Where(t => t.DonorId == donorId);

        if (collectorId is not null)
            query = query.Where(t => t.CollectorId == collectorId);

        if (!string.IsNullOrEmpty(status))
        {
            if (!EnumQuery.TryParse<TransactionStatus>(status, out var parsed, out var error))
                return Results.BadRequest(error);

            query = query.Where(t => t.Status == parsed);
        }

        return Results.Ok(await query
            .OrderByDescending(t => t.CreatedAt)
            .Select(ToDto)
            .ToListAsync(ct));
    }

    static async Task<IResult> GetAsync(
        int id, ClaimsPrincipal principal, VisszaDbContext db, CancellationToken ct)
    {
        var transaction = await db.Transactions
            .AsNoTracking()
            .Where(t => t.Id == id)
            .Select(ToDto)
            .FirstOrDefaultAsync(ct);

        if (transaction is null)
            return Results.NotFound(new MessageResponse("Transaction not found"));

        var userId = principal.GetUserId();

        if (transaction.DonorId != userId && transaction.CollectorId != userId)
            return Results.Json(new MessageResponse("Not authorized"), statusCode: 403);

        return Results.Ok(transaction);
    }

    /// <summary>
    /// Normál esetben nem ezen az úton keletkezik átvétel: a felajánló
    /// elfogadja a gyűjtési kérést, és azt az ág hozza létre. Ez a végpont
    /// korábban ellenőrzés nélkül beengedett bárkit bármelyik felajánlásra -
    /// onnan egy completed már a másik fél statisztikáját is állította.
    /// Átvételt ezért csak az nyithat, akit a felajánló kiválasztott.
    /// </summary>
    static async Task<IResult> CreateAsync(
        CreateTransactionRequest request,
        ClaimsPrincipal principal,
        VisszaDbContext db,
        CancellationToken ct)
    {
        if (request.OfferId is null)
            return Results.BadRequest(new MessageResponse("offer_id is required"));

        var userId = principal.GetUserId();

        await using var dbTransaction = await db.Database.BeginTransactionAsync(ct);

        // A felajánlás sorzárja tartja össze az ellenőrzést és a beszúrást:
        // e nélkül két párhuzamos kérés két átvételt hozna létre ugyanarra.
        var offer = await db.LockOfferAsync(request.OfferId.Value, ct);

        if (offer is null)
            return Results.NotFound(new MessageResponse("Offer not found"));

        var wasAccepted = await db.CollectionRequests.AnyAsync(
            r => r.OfferId == offer.Id
                 && r.CollectorId == userId
                 && r.Status == RequestStatus.Accepted, ct);

        if (offer.SelectedCollectorId != userId && !wasAccepted)
            return Results.Json(new MessageResponse("Not authorized"), statusCode: 403);

        var existing = await db.Transactions
            .Where(t => t.OfferId == offer.Id && t.Status != TransactionStatus.Cancelled)
            .Select(t => (int?)t.Id)
            .FirstOrDefaultAsync(ct);

        if (existing is not null)
        {
            return Results.Json(new TransactionConflictResponse
            {
                Message = "A transaction already exists for this offer",
                TransactionId = existing.Value
            }, statusCode: 409);
        }

        var entity = new Transaction
        {
            OfferId = offer.Id,
            DonorId = offer.DonorId,
            CollectorId = userId,
            PickupDate = request.PickupDate,
            Location = request.Location ?? offer.Address,
            Quantity = request.Quantity ?? offer.Quantity,
            BottleType = request.BottleType ?? offer.BottleType,

            // Egy átvétel mindig függőben nyílik. A completed induló állapot
            // megkerülné a kétoldalú megerősítést.
            Status = request.Status is TransactionStatus.Cancelled
                ? TransactionStatus.Cancelled
                : TransactionStatus.Pending
        };

        db.Transactions.Add(entity);
        await db.SaveChangesAsync(ct);
        await dbTransaction.CommitAsync(ct);

        return Results.Created($"/api/transactions/{entity.Id}",
            await LoadDtoAsync(db, entity.Id, ct));
    }

    static async Task<IResult> UpdateAsync(
        int id,
        UpdateTransactionRequest request,
        ClaimsPrincipal principal,
        VisszaDbContext db,
        CancellationToken ct)
    {
        var userId = principal.GetUserId();
        request.Status.TryGet(out var requestedStatus);

        await using var dbTransaction = await db.Database.BeginTransactionAsync(ct);

        // ZÁROLÁSI SORREND: előbb a felajánlás, utána az átvétel - ugyanaz,
        // mint a collection-requests ágon. Mindkét út írja a két táblát,
        // fordított sorrendben zárva InnoDB-holtpont lenne belőle.
        var offerId = await db.Transactions
            .AsNoTracking()
            .Where(t => t.Id == id)
            .Select(t => (int?)t.OfferId)
            .FirstOrDefaultAsync(ct);

        if (offerId is null)
            return Results.NotFound(new MessageResponse("Transaction not found"));

        await db.LockOfferOnlyAsync(offerId.Value, ct);

        // A sorzár tartja össze a státuszváltás feltételét és a hozzá tartozó
        // mellékhatásokat. E nélkül két párhuzamos completed kérés kétszer
        // növelné a számlálókat, mert mindkettő a váltás előtti sort olvassa.
        var entity = await db.LockTransactionAsync(id, ct);

        if (entity is null)
            return Results.NotFound(new MessageResponse("Transaction not found"));

        var isDonor = entity.DonorId == userId;
        var isCollector = entity.CollectorId == userId;

        if (!isDonor && !isCollector)
            return Results.Json(new MessageResponse("Not authorized"), statusCode: 403);

        var statusBefore = entity.Status;

        // A lezárt átvétel végállapot. Újranyitva a completed ág mégegyszer
        // lefutna, és a statisztika megint nőne.
        if (statusBefore == TransactionStatus.Completed
            && requestedStatus is not null
            && requestedStatus != TransactionStatus.Completed)
        {
            return Results.Json(
                new MessageResponse("A completed transaction can no longer change status"),
                statusCode: 409);
        }

        if (request.PickupDate.TryGet(out var pickupDate) && pickupDate is not null)
            entity.PickupDate = pickupDate;

        if (request.Location.TryGet(out var location) && location is not null)
            entity.Location = location;

        if (request.Quantity.TryGet(out var quantity) && quantity is not null)
            entity.Quantity = quantity.Value;

        if (request.BottleType.TryGet(out var bottleType) && bottleType is not null)
            entity.BottleType = bottleType.Value;

        // Mindkét fél csak a saját nevében erősíthet meg.
        if (isDonor && request.DonorConfirmed.TryGet(out var donorConfirmed) && donorConfirmed is not null)
            entity.DonorConfirmed = donorConfirmed.Value;

        if (isCollector && request.CollectorConfirmed.TryGet(out var collectorConfirmed) && collectorConfirmed is not null)
            entity.CollectorConfirmed = collectorConfirmed.Value;

        var result = requestedStatus switch
        {
            TransactionStatus.Completed => await CompleteAsync(db, entity, statusBefore, ct),
            TransactionStatus.Cancelled => await CancelAsync(db, entity, statusBefore, ct),
            not null => Apply(entity, requestedStatus.Value),
            null => null
        };

        if (result is not null)
            return result;

        await db.SaveChangesAsync(ct);
        await dbTransaction.CommitAsync(ct);

        return Results.Ok(await LoadDtoAsync(db, id, ct));
    }

    static IResult? Apply(Transaction entity, TransactionStatus status)
    {
        entity.Status = status;
        return null;
    }

    /// <summary>
    /// A lezárás feltétele mindkét fél megerősítése. A feltételt a friss
    /// állapoton nézzük, mert a kliens egyetlen kérésben is küldheti a saját
    /// megerősítését és a lezárást.
    /// </summary>
    static async Task<IResult?> CompleteAsync(
        VisszaDbContext db, Transaction entity, TransactionStatus statusBefore, CancellationToken ct)
    {
        // Ismételt kérés egy már lezárt átvételre: a mellékhatásokat nem
        // szabad újra lefuttatni, de hibát sem adunk.
        if (statusBefore == TransactionStatus.Completed)
            return null;

        if (!entity.DonorConfirmed || !entity.CollectorConfirmed)
        {
            return Results.BadRequest(new MessageResponse(
                "Both the donor and the collector must confirm the handover before it can be completed"));
        }

        entity.Status = TransactionStatus.Completed;
        await db.SaveChangesAsync(ct);

        await db.Offers
            .Where(o => o.Id == entity.OfferId)
            .ExecuteUpdateAsync(s => s.SetProperty(o => o.Status, OfferStatus.Completed), ct);

        // Egyetlen utasításban a két félnek. Külön UPDATE-ekkel a zárolási
        // sorrend a felek szerepétől függene, és két egyszerre lezárt átvétel
        // ugyanazon két felhasználó között holtpontot adna.
        await db.Database.ExecuteSqlRawAsync(
            """
            UPDATE `users`
               SET `successful_donations`   = `successful_donations`   + IF(`id` = {0}, 1, 0),
                   `successful_collections` = `successful_collections` + IF(`id` = {1}, 1, 0)
             WHERE `id` IN ({0}, {1})
            """,
            [entity.DonorId, entity.CollectorId], ct);

        return null;
    }

    static async Task<IResult?> CancelAsync(
        VisszaDbContext db, Transaction entity, TransactionStatus statusBefore, CancellationToken ct)
    {
        if (statusBefore == TransactionStatus.Cancelled)
            return null;

        entity.Status = TransactionStatus.Cancelled;
        await db.SaveChangesAsync(ct);

        // Ha nincs másik függő átvétel, a felajánlás újra elérhető.
        var otherPending = await db.Transactions.AnyAsync(
            t => t.OfferId == entity.OfferId
                 && t.Id != entity.Id
                 && t.Status == TransactionStatus.Pending, ct);

        if (!otherPending)
        {
            await db.Offers
                .Where(o => o.Id == entity.OfferId && o.Status != OfferStatus.Completed)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(o => o.Status, OfferStatus.Active)
                    .SetProperty(o => o.SelectedCollectorId, (int?)null), ct);
        }

        return null;
    }

    static readonly Expression<Func<Transaction, TransactionDto>> ToDto =
        t => new TransactionDto
        {
            Id = t.Id,
            OfferId = t.OfferId,
            DonorId = t.DonorId,
            CollectorId = t.CollectorId,
            PickupDate = t.PickupDate,
            Location = t.Location,
            Quantity = t.Quantity,
            BottleType = t.BottleType,
            DonorConfirmed = t.DonorConfirmed,
            CollectorConfirmed = t.CollectorConfirmed,
            Status = t.Status,
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt
        };

    static async Task<TransactionDto> LoadDtoAsync(
        VisszaDbContext db, int id, CancellationToken ct) =>
        await db.Transactions
            .AsNoTracking()
            .Where(t => t.Id == id)
            .Select(ToDto)
            .FirstAsync(ct);
}
