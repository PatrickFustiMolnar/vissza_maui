using System.Text.Json.Serialization;
using Vissza.Shared.Enums;
using Vissza.Shared.Json;

namespace Vissza.Shared.Dtos;

/// <summary>Egy gyűjtő jelentkezése egy felajánlásra.</summary>
public sealed record CollectionRequestDto
{
    public required int Id { get; init; }
    public required int OfferId { get; init; }
    public required int CollectorId { get; init; }
    public required RequestStatus Status { get; init; }
    public string? Message { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }

    public string? CollectorName { get; init; }
    public string? CollectorProfileImage { get; init; }
    public decimal? CollectorRating { get; init; }
}

/// <summary>POST /api/collection-requests</summary>
public sealed record CreateCollectionRequestRequest
{
    public int? OfferId { get; init; }
    public string? Message { get; init; }
    public RequestStatus? Status { get; init; }
}

/// <summary>PUT /api/collection-requests/{id}</summary>
public sealed record UpdateCollectionRequestRequest
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Patch<RequestStatus?> Status { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Patch<string?> Message { get; init; }
}
