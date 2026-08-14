using Microsoft.AspNetCore.SignalR.Client;
using Vissza.Shared.Dtos;

namespace Vissza.Maui.Services;

/// <summary>
/// Élő kapcsolat a beszélgetésekhez. A régi app (és a mi első változatunk is)
/// öt másodpercenként kérdezte le a szervert; ez most csak akkor mozdul, ha
/// tényleg érkezett valami.
///
/// A kapcsolat az egész alkalmazásra egy: a beszélgetés képernyője és a
/// listája ugyanarra az eseményre iratkozik fel. A hub csak értesít - üzenetet
/// továbbra is a POST /api/messages küld.
/// </summary>
public sealed class ChatHubService(AuthService auth)
{
    HubConnection? _connection;

    /// <summary>Új üzenet - a sajátunk is, ha másik eszközről ment.</summary>
    public event EventHandler<ChatMessageDto>? MessageReceived;

    /// <summary>Kapcsolódott vagy megszakadt. A hívó ettől vált lekérdezésre.</summary>
    public event EventHandler? ConnectionChanged;

    public bool IsConnected => _connection?.State == HubConnectionState.Connected;

    /// <summary>
    /// Kapcsolódás, ha még nincs. Nem dob: a hub elérhetetlensége nem hiba,
    /// csak azt jelenti, hogy marad a lekérdezés.
    /// </summary>
    public async Task StartAsync()
    {
        if (auth.Token is not { Length: > 0 })
            return;

        if (_connection is null)
            _connection = Build();

        if (_connection.State != HubConnectionState.Disconnected)
            return;

        try
        {
            await _connection.StartAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"A chat-kapcsolat nem jött létre: {ex.Message}");
        }

        ConnectionChanged?.Invoke(this, EventArgs.Empty);
    }

    HubConnection Build()
    {
        var connection = new HubConnectionBuilder()
            .WithUrl($"{ApiConfig.BaseUrl}/hubs/chat", options =>
            {
                // A WebSocket kézfogás nem tud fejlécet küldeni; a SignalR
                // ezt a tokent teszi a query stringbe. A szerver csak a hub
                // útvonalán fogadja el így.
                options.AccessTokenProvider = () => Task.FromResult(auth.Token);
            })
            // Hálózatváltásnál (wifi → mobil) a kapcsolat elszakad. Az
            // újracsatlakozás beépített; amíg tart, a hívó lekérdezésre vált.
            .WithAutomaticReconnect()
            .AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions = ApiJson.Options;
            })
            .Build();

        connection.On<ChatMessageDto>("MessageReceived", message =>
            MessageReceived?.Invoke(this, message));

        connection.Reconnected += _ =>
        {
            ConnectionChanged?.Invoke(this, EventArgs.Empty);
            return Task.CompletedTask;
        };

        connection.Closed += _ =>
        {
            ConnectionChanged?.Invoke(this, EventArgs.Empty);
            return Task.CompletedTask;
        };

        connection.Reconnecting += _ =>
        {
            ConnectionChanged?.Invoke(this, EventArgs.Empty);
            return Task.CompletedTask;
        };

        return connection;
    }

    /// <summary>
    /// Kijelentkezéskor kell: a kapcsolat a régi tokennel épült, és a
    /// következő felhasználó üzeneteit nem szabad megkapnia.
    /// </summary>
    public async Task StopAsync()
    {
        if (_connection is not { } connection)
            return;

        _connection = null;

        try
        {
            await connection.DisposeAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"A chat-kapcsolat lezárása nem sikerült: {ex.Message}");
        }
    }
}
