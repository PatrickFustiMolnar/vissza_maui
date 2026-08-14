using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace Vissza.Api.Hubs;

/// <summary>
/// A SignalR alapból a ClaimTypes.NameIdentifier claimből veszi a
/// felhasználót. A mi tokenünkben viszont "id" van - a régi backend így
/// állította ki, és az átállás alatt mindkét API ugyanazt a tokent fogadja
/// el. E nélkül a Clients.User(...) sosem találna címzettet.
/// </summary>
public sealed class ClaimUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection) =>
        connection.User?.FindFirstValue("id");
}
