using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Vissza.Api.Data;
using Vissza.Api.Services;
using Vissza.Shared.Dtos;

namespace Vissza.Api.Endpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGroup("/api/users")
            .RequireAuthorization()
            .MapGet("/{id:int}", GetAsync);
    }

    /// <summary>
    /// Más felhasználó profilja.
    ///
    /// A kapcsolattartási adatok (e-mail, telefon, lakcím és koordinátái)
    /// csak a saját profilnál mennek vissza. Idegen profilnál nem null-ként,
    /// hanem egyáltalán nem kerülnek a válaszba.
    /// </summary>
    static async Task<IResult> GetAsync(
        int id,
        ClaimsPrincipal principal,
        VisszaDbContext db,
        ImageUrlService imageUrls,
        CancellationToken ct)
    {
        var isSelf = id == principal.GetUserId();

        var profile = await db.Users
            .AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new UserProfileDto
            {
                Id = u.Id,
                Name = u.Name,
                ProfileImage = u.ProfileImage,
                UserRole = u.UserRole,
                Bio = u.Bio,
                AverageRating = u.AverageRating,
                TotalRatings = u.TotalRatings,
                SuccessfulDonations = u.SuccessfulDonations,
                SuccessfulCollections = u.SuccessfulCollections,
                CreatedAt = u.CreatedAt,

                // A szűrés a lekérdezésben van, nem utólag: ami nem a hívóé,
                // az ki sem olvasódik az adatbázisból.
                Email = isSelf ? u.Email : null,
                Phone = isSelf ? u.Phone : null,
                DefaultAddress = isSelf ? u.DefaultAddress : null,
                DefaultLat = isSelf ? u.DefaultLat : null,
                DefaultLng = isSelf ? u.DefaultLng : null
            })
            .FirstOrDefaultAsync(ct);

        return profile is null
            ? Results.NotFound(new MessageResponse("User not found"))
            : Results.Ok(profile with { ProfileImage = imageUrls.ToAbsolute(profile.ProfileImage) });
    }
}
