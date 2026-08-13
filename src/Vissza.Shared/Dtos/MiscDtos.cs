using System.Text.Json.Serialization;
using Vissza.Shared.Enums;

namespace Vissza.Shared.Dtos;

/// <summary>
/// A felhasználó rövid képe, ahogy listákban és beszélgetésekben megjelenik.
/// E-mail nincs benne: más felhasználók elérhetősége nem tartozik a hívóra.
/// </summary>
public sealed record UserSummaryDto
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public string? ProfileImage { get; init; }
    public decimal AverageRating { get; init; }
}

/// <summary>
/// GET /api/users/{id} - más felhasználó profilja.
///
/// A kapcsolattartási adatok (e-mail, telefon, lakcím) csak a saját profilnál
/// mennek vissza. Idegen profilnál ezek a mezők null-ok, és a
/// WhenWritingNull miatt ki sem kerülnek a JSON-be - pontosan úgy, ahogy a
/// régi API sem küldte őket.
/// </summary>
public sealed record UserProfileDto
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public string? ProfileImage { get; init; }
    public required UserRole UserRole { get; init; }
    public string? Bio { get; init; }
    public decimal AverageRating { get; init; }
    public int TotalRatings { get; init; }
    public int SuccessfulDonations { get; init; }
    public int SuccessfulCollections { get; init; }
    public DateTime CreatedAt { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Email { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Phone { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DefaultAddress { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? DefaultLat { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? DefaultLng { get; init; }
}

/// <summary>Egy visszaváltó hely.</summary>
public sealed record ReturnLocationDto
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required string Address { get; init; }
    public required decimal Lat { get; init; }
    public required decimal Lng { get; init; }
    public required LocationType Type { get; init; }
    public string? OpeningHours { get; init; }

    /// <summary>Vesszővel elválasztott palacktípusok, pl. "pet,glass".</summary>
    public string? AcceptedTypes { get; init; }

    public string? Contact { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

/// <summary>POST /api/upload</summary>
public sealed record UploadResponse
{
    public required string Message { get; init; }

    /// <summary>Relatív útvonal, pl. /uploads/file-123.jpg</summary>
    public required string Url { get; init; }

    public required string Filename { get; init; }
}
