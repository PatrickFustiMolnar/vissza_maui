using System.Text.Json.Serialization;

namespace Vissza.Shared.Dtos;

/// <summary>Egy üzenet.</summary>
public sealed record ChatMessageDto
{
    public required int Id { get; init; }
    public int? OfferId { get; init; }
    public required int SenderId { get; init; }
    public required int ReceiverId { get; init; }
    public required string Content { get; init; }

    /// <summary>Az oszlop neve a sémában `read`, ezért itt is az.</summary>
    [JsonPropertyName("read")]
    public required bool IsRead { get; init; }

    public DateTime CreatedAt { get; init; }
}

/// <summary>POST /api/messages</summary>
public sealed record SendMessageRequest
{
    public int? OfferId { get; init; }
    public int? ReceiverId { get; init; }
    public string? Content { get; init; }
}

/// <summary>
/// Egy beszélgetés a partner adataival és az utolsó üzenettel.
/// </summary>
public sealed record ConversationDto
{
    public UserSummaryDto? Partner { get; init; }
    public ChatMessageDto? LastMessage { get; init; }
    public required int UnreadCount { get; init; }
    public DateTime? LastMessageAt { get; init; }
}

/// <summary>GET /api/messages/unread-count</summary>
public sealed record UnreadCountResponse
{
    public required int Count { get; init; }
}
