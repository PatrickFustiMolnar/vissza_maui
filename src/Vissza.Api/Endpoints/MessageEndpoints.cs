using System.Linq.Expressions;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vissza.Api.Data;
using Vissza.Api.Entities;
using Vissza.Api.Services;
using Vissza.Shared.Dtos;

namespace Vissza.Api.Endpoints;

public static class MessageEndpoints
{
    public static void MapMessageEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/messages").RequireAuthorization();

        // A konkrét útvonalak a paraméteresek elé kellenek, különben az
        // "unread-count" beleesne egy /{id} mintába.
        group.MapGet("/unread-count", UnreadCountAsync);
        group.MapGet("/conversations", ConversationsAsync);

        group.MapGet("/", ListAsync);
        group.MapPost("/", SendAsync);
        group.MapPut("/{id:int}/read", MarkReadAsync);
        group.MapDelete("/{id:int}", DeleteAsync);
    }

    static async Task<IResult> ListAsync(
        ClaimsPrincipal principal,
        VisszaDbContext db,
        CancellationToken ct,
        [FromQuery(Name = "offer_id")] int? offerId = null,
        [FromQuery(Name = "sender_id")] int? senderId = null,
        [FromQuery(Name = "receiver_id")] int? receiverId = null)
    {
        var userId = principal.GetUserId();

        // Csak a saját üzeneteid. A hívó szűrői ezen belül hatnak.
        var query = db.Messages
            .AsNoTracking()
            .Where(m => m.SenderId == userId || m.ReceiverId == userId);

        if (offerId is not null)
            query = query.Where(m => m.OfferId == offerId);

        if (senderId is not null)
            query = query.Where(m => m.SenderId == senderId);

        if (receiverId is not null)
            query = query.Where(m => m.ReceiverId == receiverId);

        return Results.Ok(await query
            .OrderBy(m => m.CreatedAt)
            .Select(ToDto)
            .ToListAsync(ct));
    }

    static async Task<IResult> SendAsync(
        SendMessageRequest request,
        ClaimsPrincipal principal,
        VisszaDbContext db,
        CancellationToken ct)
    {
        if (request.ReceiverId is null || string.IsNullOrWhiteSpace(request.Content))
            return Results.BadRequest(new MessageResponse("receiver_id and content are required"));

        var entity = new Message
        {
            OfferId = request.OfferId,
            SenderId = principal.GetUserId(),
            ReceiverId = request.ReceiverId.Value,
            Content = request.Content
        };

        db.Messages.Add(entity);
        await db.SaveChangesAsync(ct);

        var dto = await db.Messages
            .AsNoTracking()
            .Where(m => m.Id == entity.Id)
            .Select(ToDto)
            .FirstAsync(ct);

        return Results.Created($"/api/messages/{entity.Id}", dto);
    }

    static async Task<IResult> UnreadCountAsync(
        ClaimsPrincipal principal, VisszaDbContext db, CancellationToken ct)
    {
        var userId = principal.GetUserId();

        var count = await db.Messages
            .CountAsync(m => m.ReceiverId == userId && !m.IsRead, ct);

        return Results.Ok(new UnreadCountResponse { Count = count });
    }

    /// <summary>
    /// Beszélgetéslista: partnerenként az utolsó üzenet és az olvasatlanok
    /// száma.
    ///
    /// Két lekérdezés összesen, nem partnerenként kettő. A régi megoldás
    /// elemenként kérdezte le a partnert és az utolsó üzenetet, ami egy
    /// távoli adatbázison 1 + 2N körre futott.
    /// </summary>
    static async Task<IResult> ConversationsAsync(
        ClaimsPrincipal principal,
        VisszaDbContext db,
        ImageUrlService imageUrls,
        CancellationToken ct)
    {
        var userId = principal.GetUserId();

        var mine = db.Messages
            .AsNoTracking()
            .Where(m => m.SenderId == userId || m.ReceiverId == userId)
            .Select(m => new
            {
                Message = m,
                PartnerId = m.SenderId == userId ? m.ReceiverId : m.SenderId
            });

        var summaries = await mine
            .GroupBy(x => x.PartnerId)
            .Select(g => new
            {
                PartnerId = g.Key,
                LastMessageAt = g.Max(x => x.Message.CreatedAt),
                UnreadCount = g.Count(x => x.Message.ReceiverId == userId && !x.Message.IsRead),

                // Azonos időbélyegnél az id dönt, hogy az eredmény
                // determinisztikus legyen.
                LastMessageId = g.OrderByDescending(x => x.Message.CreatedAt)
                                 .ThenByDescending(x => x.Message.Id)
                                 .Select(x => x.Message.Id)
                                 .First()
            })
            .OrderByDescending(x => x.LastMessageAt)
            .ToListAsync(ct);

        var lastMessageIds = summaries.Select(s => s.LastMessageId).ToList();
        var partnerIds = summaries.Select(s => s.PartnerId).ToList();

        var lastMessages = await db.Messages
            .AsNoTracking()
            .Where(m => lastMessageIds.Contains(m.Id))
            .Select(ToDto)
            .ToDictionaryAsync(m => m.Id, ct);

        var partners = await db.Users
            .AsNoTracking()
            .Where(u => partnerIds.Contains(u.Id))
            .Select(u => new UserSummaryDto
            {
                Id = u.Id,
                Name = u.Name,
                ProfileImage = u.ProfileImage,
                AverageRating = u.AverageRating
            })
            .ToDictionaryAsync(u => u.Id, ct);

        var conversations = summaries.Select(s => new ConversationDto
        {
            Partner = partners.TryGetValue(s.PartnerId, out var partner)
                ? partner with { ProfileImage = imageUrls.ToAbsolute(partner.ProfileImage) }
                : null,
            LastMessage = lastMessages.GetValueOrDefault(s.LastMessageId),
            UnreadCount = s.UnreadCount,
            LastMessageAt = s.LastMessageAt
        });

        return Results.Ok(conversations);
    }

    static async Task<IResult> MarkReadAsync(
        int id, ClaimsPrincipal principal, VisszaDbContext db, CancellationToken ct)
    {
        var entity = await db.Messages.FirstOrDefaultAsync(m => m.Id == id, ct);

        if (entity is null)
            return Results.NotFound(new MessageResponse("Message not found"));

        // Olvasottnak jelölni csak a címzett tud.
        if (entity.ReceiverId != principal.GetUserId())
            return Results.Json(new MessageResponse("Not authorized"), statusCode: 403);

        entity.IsRead = true;
        await db.SaveChangesAsync(ct);

        return Results.Ok(await db.Messages
            .AsNoTracking()
            .Where(m => m.Id == id)
            .Select(ToDto)
            .FirstAsync(ct));
    }

    static async Task<IResult> DeleteAsync(
        int id, ClaimsPrincipal principal, VisszaDbContext db, CancellationToken ct)
    {
        var entity = await db.Messages.FirstOrDefaultAsync(m => m.Id == id, ct);

        if (entity is null)
            return Results.NotFound(new MessageResponse("Message not found"));

        if (entity.SenderId != principal.GetUserId())
        {
            return Results.Json(new MessageResponse(
                "Not authorized. You can only delete your own messages."), statusCode: 403);
        }

        db.Messages.Remove(entity);
        await db.SaveChangesAsync(ct);

        return Results.Ok(new MessageResponse("Message deleted successfully"));
    }

    static readonly Expression<Func<Message, ChatMessageDto>> ToDto =
        m => new ChatMessageDto
        {
            Id = m.Id,
            OfferId = m.OfferId,
            SenderId = m.SenderId,
            ReceiverId = m.ReceiverId,
            Content = m.Content,
            IsRead = m.IsRead,
            CreatedAt = m.CreatedAt
        };
}
