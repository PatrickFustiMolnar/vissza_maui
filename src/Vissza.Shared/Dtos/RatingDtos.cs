using System.Text.Json.Serialization;
using Vissza.Shared.Json;

namespace Vissza.Shared.Dtos;

/// <summary>
/// Egy értékelés, az értékelő nevével és képével.
///
/// Az értékelő e-mail címe szándékosan nincs benne. A régi API a listából
/// már kihagyta ("az értékelő e-mail címe nem tartozik a hívóra"), a
/// GET /:id és a POST viszont még kiadta - a kliens pedig sehol nem
/// használta. Itt mindhárom végpont egyformán viselkedik.
/// </summary>
public sealed record RatingDto
{
    public required int Id { get; init; }
    public required int RaterId { get; init; }
    public required int RatedId { get; init; }
    public int? TransactionId { get; init; }
    public required int Stars { get; init; }
    public string? Comment { get; init; }
    public DateTime CreatedAt { get; init; }

    public string? RaterName { get; init; }
    public string? RaterProfileImage { get; init; }
}

/// <summary>POST /api/ratings</summary>
public sealed record CreateRatingRequest
{
    public int? RatedId { get; init; }
    public int? TransactionId { get; init; }
    public int? Stars { get; init; }
    public string? Comment { get; init; }
}

/// <summary>PUT /api/ratings/{id}</summary>
public sealed record UpdateRatingRequest
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Patch<int?> Stars { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Patch<string?> Comment { get; init; }
}
