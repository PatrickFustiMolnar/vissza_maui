using System.Linq.Expressions;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vissza.Api.Data;
using Vissza.Api.Entities;
using Vissza.Api.Services;
using Vissza.Shared.Dtos;

namespace Vissza.Api.Endpoints;

public static class RatingEndpoints
{
    public static void MapRatingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ratings").RequireAuthorization();

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
        [FromQuery(Name = "rated_id")] int? ratedId = null,
        [FromQuery(Name = "rater_id")] int? raterId = null,
        [FromQuery(Name = "transaction_id")] int? transactionId = null)
    {
        var query = db.Ratings.AsNoTracking();

        if (ratedId is not null)
            query = query.Where(r => r.RatedId == ratedId);

        if (raterId is not null)
            query = query.Where(r => r.RaterId == raterId);

        if (transactionId is not null)
            query = query.Where(r => r.TransactionId == transactionId);

        var ratings = await query
            .OrderByDescending(r => r.CreatedAt)
            .Select(ToDto)
            .ToListAsync(ct);

        return Results.Ok(ratings.Select(r => Absolute(r, imageUrls)));
    }

    static async Task<IResult> GetAsync(
        int id, VisszaDbContext db, ImageUrlService imageUrls, CancellationToken ct)
    {
        var rating = await db.Ratings
            .AsNoTracking()
            .Where(r => r.Id == id)
            .Select(ToDto)
            .FirstOrDefaultAsync(ct);

        return rating is null
            ? Results.NotFound(new MessageResponse("Rating not found"))
            : Results.Ok(Absolute(rating, imageUrls));
    }

    static async Task<IResult> CreateAsync(
        CreateRatingRequest request,
        ClaimsPrincipal principal,
        VisszaDbContext db,
        ImageUrlService imageUrls,
        CancellationToken ct)
    {
        if (request.RatedId is null || request.Stars is not (>= 1 and <= 5))
            return Results.BadRequest(new MessageResponse("rated_id and stars (1-5) are required"));

        var userId = principal.GetUserId();

        if (request.TransactionId is not null)
        {
            var duplicate = await db.Ratings.AnyAsync(
                r => r.TransactionId == request.TransactionId
                     && r.RaterId == userId
                     && r.RatedId == request.RatedId, ct);

            if (duplicate)
            {
                return Results.BadRequest(
                    new MessageResponse("Rating already exists for this transaction"));
            }
        }

        var entity = new Rating
        {
            RaterId = userId,
            RatedId = request.RatedId.Value,
            TransactionId = request.TransactionId,
            Stars = request.Stars.Value,
            Comment = request.Comment
        };

        db.Ratings.Add(entity);
        await db.SaveChangesAsync(ct);

        await RecalculateAverageAsync(db, entity.RatedId, ct);

        var dto = await LoadDtoAsync(db, entity.Id, ct);

        return Results.Created($"/api/ratings/{entity.Id}", Absolute(dto, imageUrls));
    }

    static async Task<IResult> UpdateAsync(
        int id,
        UpdateRatingRequest request,
        ClaimsPrincipal principal,
        VisszaDbContext db,
        ImageUrlService imageUrls,
        CancellationToken ct)
    {
        var entity = await db.Ratings.FirstOrDefaultAsync(r => r.Id == id, ct);

        if (entity is null)
            return Results.NotFound(new MessageResponse("Rating not found"));

        if (entity.RaterId != principal.GetUserId())
            return Results.Json(new MessageResponse("Not authorized"), statusCode: 403);

        if (request.Stars.TryGet(out var stars) && stars is not null)
        {
            if (stars is < 1 or > 5)
                return Results.BadRequest(new MessageResponse("Stars must be between 1 and 5"));

            entity.Stars = stars.Value;
        }

        if (request.Comment.TryGet(out var comment))
            entity.Comment = comment;

        await db.SaveChangesAsync(ct);
        await RecalculateAverageAsync(db, entity.RatedId, ct);

        return Results.Ok(Absolute(await LoadDtoAsync(db, id, ct), imageUrls));
    }

    static async Task<IResult> DeleteAsync(
        int id, ClaimsPrincipal principal, VisszaDbContext db, CancellationToken ct)
    {
        var entity = await db.Ratings.FirstOrDefaultAsync(r => r.Id == id, ct);

        if (entity is null)
            return Results.NotFound(new MessageResponse("Rating not found"));

        if (entity.RaterId != principal.GetUserId())
            return Results.Json(new MessageResponse("Not authorized"), statusCode: 403);

        var ratedId = entity.RatedId;

        db.Ratings.Remove(entity);
        await db.SaveChangesAsync(ct);

        await RecalculateAverageAsync(db, ratedId, ct);

        return Results.Ok(new MessageResponse("Rating deleted successfully"));
    }

    /// <summary>
    /// Az értékelt felhasználó átlaga és darabszáma egyetlen UPDATE-ben,
    /// az értékelésekből számolva. Nem inkrementálisan: így egy elmaradt
    /// frissítés sem tudja tartósan elrontani a számot.
    /// </summary>
    static async Task RecalculateAverageAsync(VisszaDbContext db, int ratedId, CancellationToken ct)
    {
        await db.Database.ExecuteSqlRawAsync(
            """
            UPDATE `users` u
               SET u.`average_rating` = COALESCE(
                       (SELECT AVG(r.`stars`) FROM `ratings` r WHERE r.`rated_id` = u.`id`), 0),
                   u.`total_ratings`  =
                       (SELECT COUNT(*)      FROM `ratings` r WHERE r.`rated_id` = u.`id`)
             WHERE u.`id` = {0}
            """,
            [ratedId], ct);
    }

    static RatingDto Absolute(RatingDto dto, ImageUrlService imageUrls) =>
        dto with { RaterProfileImage = imageUrls.ToAbsolute(dto.RaterProfileImage) };

    static readonly Expression<Func<Rating, RatingDto>> ToDto =
        r => new RatingDto
        {
            Id = r.Id,
            RaterId = r.RaterId,
            RatedId = r.RatedId,
            TransactionId = r.TransactionId,
            Stars = r.Stars,
            Comment = r.Comment,
            CreatedAt = r.CreatedAt,
            RaterName = r.Rater.Name,
            RaterProfileImage = r.Rater.ProfileImage
        };

    static async Task<RatingDto> LoadDtoAsync(VisszaDbContext db, int id, CancellationToken ct) =>
        await db.Ratings.AsNoTracking().Where(r => r.Id == id).Select(ToDto).FirstAsync(ct);
}
