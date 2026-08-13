using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vissza.Api.Data;
using Vissza.Api.Entities;
using Vissza.Shared.Dtos;
using Vissza.Shared.Enums;

namespace Vissza.Api.Endpoints;

public static class ReturnLocationEndpoints
{
    public static void MapReturnLocationEndpoints(this IEndpointRouteBuilder app)
    {
        // Az egyetlen csoport, ami token nélkül is hívható: a visszaváltó
        // helyek nyilvános adatok, és a térkép a bejelentkezés előtt is
        // meg tudja jeleníteni őket.
        var group = app.MapGroup("/api/return-locations").AllowAnonymous();

        group.MapGet("/", ListAsync);
        group.MapGet("/{id:int}", GetAsync);
    }

    static async Task<IResult> ListAsync(
        VisszaDbContext db,
        CancellationToken ct,
        [FromQuery(Name = "type")] string? type = null)
    {
        var query = db.ReturnLocations.AsNoTracking();

        if (!string.IsNullOrEmpty(type))
        {
            if (!EnumQuery.TryParse<LocationType>(type, out var parsed, out var error))
                return Results.BadRequest(error);

            query = query.Where(l => l.Type == parsed);
        }

        return Results.Ok(await query
            .OrderBy(l => l.Name)
            .Select(ToDto)
            .ToListAsync(ct));
    }

    static async Task<IResult> GetAsync(int id, VisszaDbContext db, CancellationToken ct)
    {
        var location = await db.ReturnLocations
            .AsNoTracking()
            .Where(l => l.Id == id)
            .Select(ToDto)
            .FirstOrDefaultAsync(ct);

        return location is null
            ? Results.NotFound(new MessageResponse("Return location not found"))
            : Results.Ok(location);
    }

    static readonly Expression<Func<ReturnLocation, ReturnLocationDto>> ToDto =
        l => new ReturnLocationDto
        {
            Id = l.Id,
            Name = l.Name,
            Address = l.Address,
            Lat = l.Lat,
            Lng = l.Lng,
            Type = l.Type,
            OpeningHours = l.OpeningHours,
            AcceptedTypes = l.AcceptedTypes,
            Contact = l.Contact,
            CreatedAt = l.CreatedAt,
            UpdatedAt = l.UpdatedAt
        };
}
