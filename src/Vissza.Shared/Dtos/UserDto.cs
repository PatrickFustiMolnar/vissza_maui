using Vissza.Shared.Enums;

namespace Vissza.Shared.Dtos;

/// <summary>
/// A felhasználó nyilvános képe. Mezőnként megegyezik azzal, amit a régi
/// Express backend adott vissza - a JSON snake_case nevek a globális
/// névpolitikából jönnek, nem attribútumokból.
///
/// A password_hash szándékosan nincs benne: így nem lehet véletlenül kiadni.
/// </summary>
public sealed record UserDto
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required string Email { get; init; }
    public string? Phone { get; init; }

    /// <summary>Teljes URL, nem relatív útvonal - lásd ImageUrlService.</summary>
    public string? ProfileImage { get; init; }

    public required UserRole UserRole { get; init; }
    public string? Bio { get; init; }
    public string? DefaultAddress { get; init; }
    public decimal? DefaultLat { get; init; }
    public decimal? DefaultLng { get; init; }

    public decimal AverageRating { get; init; }
    public int TotalRatings { get; init; }
    public int SuccessfulDonations { get; init; }
    public int SuccessfulCollections { get; init; }

    public bool NotificationsEnabled { get; init; }

    /// <summary>Kilométerben.</summary>
    public int NotificationRadius { get; init; }

    public bool DarkMode { get; init; }

    public DateTime CreatedAt { get; init; }
    public DateTime? LastActivity { get; init; }
}
