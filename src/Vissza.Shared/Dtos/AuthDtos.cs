using Vissza.Shared.Enums;

namespace Vissza.Shared.Dtos;

/// <summary>POST /api/auth/register</summary>
public sealed record RegisterRequest
{
    public string? Name { get; init; }
    public string? Email { get; init; }
    public string? Password { get; init; }
    public string? Phone { get; init; }

    /// <summary>Elhagyható; alapértelmezés <see cref="UserRole.Both"/>.</summary>
    public UserRole? UserRole { get; init; }
}

/// <summary>POST /api/auth/login</summary>
public sealed record LoginRequest
{
    public string? Email { get; init; }
    public string? Password { get; init; }
}

/// <summary>A register és a login közös válasza.</summary>
public sealed record AuthResponse
{
    public required string Message { get; init; }
    public required string Token { get; init; }
    public required UserDto User { get; init; }
}

/// <summary>
/// PUT /api/auth/me - részleges frissítés.
///
/// Minden mező nullable, és a null azt jelenti, hogy "ezt ne módosítsd".
/// Ez a régi backend viselkedése, amit meg kell tartani: a kliens sosem
/// küldi el a teljes profilt, csak az érintett mezőket.
///
/// Következmény: a nullázható mezőket (phone, bio, ...) ezen a végponton
/// nem lehet null-ra állítani, csak üres sztringre. A régi API sem tudta.
/// </summary>
public sealed record UpdateProfileRequest
{
    public string? Name { get; init; }
    public string? Phone { get; init; }
    public string? Bio { get; init; }
    public UserRole? UserRole { get; init; }
    public string? DefaultAddress { get; init; }
    public decimal? DefaultLat { get; init; }
    public decimal? DefaultLng { get; init; }
    public string? ProfileImage { get; init; }
    public bool? NotificationsEnabled { get; init; }
    public int? NotificationRadius { get; init; }
    public bool? DarkMode { get; init; }
}

/// <summary>A hibaválaszok egységes alakja: { "message": "..." }</summary>
public sealed record MessageResponse(string Message);
