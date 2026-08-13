using System.Text.Json.Serialization;
using Vissza.Shared.Enums;
using Vissza.Shared.Json;

namespace Vissza.Shared.Dtos;

/// <summary>Egy átvétel. Csak a két érintett fél láthatja.</summary>
public sealed record TransactionDto
{
    public required int Id { get; init; }
    public required int OfferId { get; init; }
    public required int DonorId { get; init; }
    public required int CollectorId { get; init; }
    public DateTime? PickupDate { get; init; }
    public string? Location { get; init; }
    public required int Quantity { get; init; }
    public required BottleType BottleType { get; init; }
    public required bool DonorConfirmed { get; init; }
    public required bool CollectorConfirmed { get; init; }
    public required TransactionStatus Status { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

/// <summary>POST /api/transactions</summary>
public sealed record CreateTransactionRequest
{
    public int? OfferId { get; init; }
    public DateTime? PickupDate { get; init; }
    public string? Location { get; init; }
    public int? Quantity { get; init; }
    public BottleType? BottleType { get; init; }
    public TransactionStatus? Status { get; init; }
}

/// <summary>PUT /api/transactions/{id}</summary>
public sealed record UpdateTransactionRequest
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Patch<DateTime?> PickupDate { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Patch<string?> Location { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Patch<int?> Quantity { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Patch<BottleType?> BottleType { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Patch<bool?> DonorConfirmed { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Patch<bool?> CollectorConfirmed { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Patch<TransactionStatus?> Status { get; init; }
}

/// <summary>
/// A 409-es válasz, ha egy felajánláshoz már tartozik nyitott átvétel.
/// A kliensnek szüksége van a meglévő azonosítójára, hogy oda navigáljon.
/// </summary>
public sealed record TransactionConflictResponse
{
    public required string Message { get; init; }
    public required int TransactionId { get; init; }
}
