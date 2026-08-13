using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Vissza.Api.Data;
using Vissza.Api.Entities;
using Vissza.Api.Mapping;
using Vissza.Api.Services;
using Vissza.Shared.Dtos;
using Vissza.Shared.Enums;

namespace Vissza.Api.Endpoints;

public static class AuthEndpoints
{
    /// <summary>
    /// A bcrypt munkatényező az új jelszavakhoz. A régi backend 10-zel írt;
    /// a tényező a hashben tárolódik, ezért a régi jelszavak ellenőrzése
    /// ettől függetlenül működik.
    /// </summary>
    const int WorkFactor = 11;

    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth");

        group.MapPost("/register", RegisterAsync).AllowAnonymous();
        group.MapPost("/login", LoginAsync).AllowAnonymous();
        group.MapGet("/me", GetMeAsync).RequireAuthorization();
        group.MapPut("/me", UpdateMeAsync).RequireAuthorization();
    }

    static async Task<IResult> RegisterAsync(
        RegisterRequest request,
        VisszaDbContext db,
        JwtService jwt,
        ImageUrlService imageUrls,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name)
            || string.IsNullOrWhiteSpace(request.Email)
            || string.IsNullOrWhiteSpace(request.Password))
        {
            return Results.BadRequest(
                new MessageResponse("Name, email, and password are required"));
        }

        var email = request.Email.Trim();

        if (await db.Users.AnyAsync(u => u.Email == email, ct))
            return Results.BadRequest(new MessageResponse("User already exists"));

        var user = new User
        {
            Name = request.Name.Trim(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, WorkFactor),
            Phone = request.Phone,
            UserRole = request.UserRole ?? UserRole.Both
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/users/{user.Id}", new AuthResponse
        {
            Message = "User created successfully",
            Token = jwt.CreateToken(user.Id, user.Email),
            User = user.ToDto(imageUrls)
        });
    }

    static async Task<IResult> LoginAsync(
        LoginRequest request,
        VisszaDbContext db,
        JwtService jwt,
        ImageUrlService imageUrls,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Results.BadRequest(
                new MessageResponse("Email and password are required"));
        }

        var email = request.Email.Trim();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

        // Ismeretlen e-mail és rossz jelszó ugyanazt a választ adja: különben
        // a végpont megmondaná, mely e-mail címek vannak regisztrálva.
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Results.Json(new MessageResponse("Invalid credentials"), statusCode: 401);

        return Results.Ok(new AuthResponse
        {
            Message = "Login successful",
            Token = jwt.CreateToken(user.Id, user.Email),
            User = user.ToDto(imageUrls)
        });
    }

    static async Task<IResult> GetMeAsync(
        ClaimsPrincipal principal,
        VisszaDbContext db,
        ImageUrlService imageUrls,
        CancellationToken ct)
    {
        var userId = principal.GetUserId();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user is null)
            return Results.NotFound(new MessageResponse("User not found"));

        // Külön UPDATE, nem a SaveChanges része: az updated_at oszlopot nem
        // akarjuk elmozdítani attól, hogy valaki megnyitotta az appot.
        await db.Users
            .Where(u => u.Id == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.LastActivity, DateTime.UtcNow), ct);

        return Results.Ok(user.ToDto(imageUrls));
    }

    static async Task<IResult> UpdateMeAsync(
        UpdateProfileRequest request,
        ClaimsPrincipal principal,
        VisszaDbContext db,
        ImageUrlService imageUrls,
        CancellationToken ct)
    {
        // Kiemelve a lekérdezésből: a kifejezésfában hagyva az EF-nek kellene
        // eldöntenie, hogy lefordítja vagy kliensoldalon értékeli ki.
        var userId = principal.GetUserId();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user is null)
            return Results.NotFound(new MessageResponse("User not found"));

        // A null azt jelenti, hogy "ne módosítsd" - lásd UpdateProfileRequest.
        var touched = false;

        if (request.Name is not null) { user.Name = request.Name; touched = true; }
        if (request.Phone is not null) { user.Phone = request.Phone; touched = true; }
        if (request.Bio is not null) { user.Bio = request.Bio; touched = true; }
        if (request.UserRole is not null) { user.UserRole = request.UserRole.Value; touched = true; }
        if (request.DefaultAddress is not null) { user.DefaultAddress = request.DefaultAddress; touched = true; }
        if (request.DefaultLat is not null) { user.DefaultLat = request.DefaultLat; touched = true; }
        if (request.DefaultLng is not null) { user.DefaultLng = request.DefaultLng; touched = true; }
        if (request.ProfileImage is not null) { user.ProfileImage = request.ProfileImage; touched = true; }
        if (request.NotificationsEnabled is not null) { user.NotificationsEnabled = request.NotificationsEnabled.Value; touched = true; }
        if (request.NotificationRadius is not null) { user.NotificationRadius = request.NotificationRadius.Value; touched = true; }
        if (request.DarkMode is not null) { user.DarkMode = request.DarkMode.Value; touched = true; }

        if (!touched)
            return Results.BadRequest(new MessageResponse("No fields to update"));

        await db.SaveChangesAsync(ct);

        return Results.Ok(user.ToDto(imageUrls));
    }
}
