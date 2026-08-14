using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Vissza.Api.Hubs;

/// <summary>
/// Az élő beszélgetés csatornája.
///
/// Szándékosan nincs egyetlen hívható metódusa sem: a kliens csak fogad, az
/// írás útja marad a POST /api/messages. Így az ellenőrzés, a mentés és a
/// hibakezelés egy helyen van, a hub pedig csak értesít - egy hub-metódussal
/// mindez meg lenne duplázva.
/// </summary>
[Authorize]
public sealed class ChatHub : Hub
{
    /// <summary>Az esemény neve, amit a kliens figyel.</summary>
    public const string MessageReceived = "MessageReceived";
}
