using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vissza.Maui.Services;
using Vissza.Shared.Dtos;

namespace Vissza.Maui.ViewModels;

/// <summary>
/// Egy beszélgetés. A ChatScreen.js leképezése.
///
/// Élőben a SignalR hozza az üzeneteket, tehát a képernyő csak akkor mozdul,
/// ha tényleg érkezett valami. A régi app (és a mi első változatunk is) öt
/// másodpercenként kérdezte a szervert - az időzítő most tartaléknak marad
/// arra az esetre, ha a hub nem érhető el.
/// </summary>
public sealed partial class ChatViewModel(
    IServiceProvider services, AuthService auth, ChatHubService hub) : ViewModelBase
{
    /// <summary>
    /// A tartalék lekérdezés üteme. Ritkább, mint a régi öt másodperc: ez már
    /// nem az elsődleges út, csak akkor fut, ha az élő kapcsolat nincs meg.
    /// </summary>
    static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(10);

    IVisszaApi Api => services.GetRequiredService<IVisszaApi>();

    CancellationTokenSource? _polling;
    bool _subscribed;

    public ObservableCollection<ChatMessageItem> Messages { get; } = [];

    public int CurrentUserId => auth.CurrentUser?.Id ?? 0;

    [ObservableProperty]
    public partial int PartnerId { get; set; }

    [ObservableProperty]
    public partial string PartnerName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Draft { get; set; } = string.Empty;

    public async Task LoadAsync()
    {
        await RunAsync(async () =>
        {
            // Egyetlen célzott lekérdezés: a szerver a partner_id
            // paraméterrel mindkét irányt visszaadja. A régi kliens a teljes
            // postafiókot töltötte le, és kliensoldalon szűrt.
            var messages = await Api.GetMessagesAsync(partnerId: PartnerId);

            Replace(messages);
        });

        await MarkIncomingReadAsync();
    }

    /// <summary>
    /// A nekem címzett, még olvasatlan üzenetek megjelölése. Nincs tömeges
    /// végpont, ezért darabonként megy - de csak azokra, amik tényleg
    /// olvasatlanok.
    /// </summary>
    async Task MarkIncomingReadAsync()
    {
        var unread = Messages
            .Where(m => m.ReceiverId == CurrentUserId && !m.IsRead)
            .ToList();

        foreach (var message in unread)
        {
            try
            {
                await Api.MarkMessageReadAsync(message.Id);
            }
            catch (Exception ex)
            {
                // Az olvasottság nem kritikus: ha elbukik, a következő
                // körben újra megpróbáljuk.
                System.Diagnostics.Debug.WriteLine($"Olvasottnak jelölés nem sikerült: {ex.Message}");
            }
        }
    }

    void Replace(IReadOnlyList<ChatMessageDto> messages)
    {
        Messages.Clear();

        foreach (var message in messages)
            Messages.Add(new ChatMessageItem(message, message.SenderId == CurrentUserId));
    }

    // --- élő kapcsolat ---

    /// <summary>A képernyő megnyitásakor fut: élő kapcsolat, vagy tartalék.</summary>
    public async Task ListenAsync()
    {
        if (!_subscribed)
        {
            hub.MessageReceived += OnHubMessage;
            hub.ConnectionChanged += OnConnectionChanged;
            _subscribed = true;
        }

        await hub.StartAsync();

        ChooseTransport();
    }

    public void StopListening()
    {
        if (_subscribed)
        {
            hub.MessageReceived -= OnHubMessage;
            hub.ConnectionChanged -= OnConnectionChanged;
            _subscribed = false;
        }

        StopPolling();
    }

    /// <summary>
    /// Élő kapcsolattal nincs lekérdezés, nélküle van. A kettő sosem fut
    /// egyszerre.
    /// </summary>
    void ChooseTransport()
    {
        if (hub.IsConnected)
            StopPolling();
        else
            StartPolling();
    }

    void OnConnectionChanged(object? sender, EventArgs e) =>
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            ChooseTransport();

            // Újracsatlakozás után lehet, hogy lemaradtunk üzenetekről: amíg
            // nem volt kapcsolat, a hub nem tudta kézbesíteni őket.
            if (hub.IsConnected)
                await RefreshQuietlyAsync();
        });

    void OnHubMessage(object? sender, ChatMessageDto message)
    {
        // Csak ez a beszélgetés érdekel. Másik partnertől érkező üzenetet a
        // beszélgetéslista kezel.
        if (message.SenderId != PartnerId && message.ReceiverId != PartnerId)
            return;

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            // A saját üzenetünket a küldés már betette; a hub a többi
            // eszközünk miatt nekünk is elküldi.
            if (Messages.Any(m => m.Id == message.Id))
                return;

            Messages.Add(new ChatMessageItem(message, message.SenderId == CurrentUserId));

            if (message.ReceiverId == CurrentUserId)
                await MarkIncomingReadAsync();
        });
    }

    // --- tartalék lekérdezés ---

    void StartPolling()
    {
        if (_polling is not null)
            return;

        _polling = new CancellationTokenSource();
        _ = PollAsync(_polling.Token);
    }

    void StopPolling()
    {
        _polling?.Cancel();
        _polling?.Dispose();
        _polling = null;
    }

    async Task PollAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(PollInterval);

        try
        {
            while (await timer.WaitForNextTickAsync(ct))
                await RefreshQuietlyAsync();
        }
        catch (OperationCanceledException)
        {
            // A képernyő elhagyása vagy az élő kapcsolat megjötte - nem hiba.
        }
    }

    /// <summary>
    /// Háttérfrissítés: nem villogtatja a betöltésjelzőt, és a hibáját sem
    /// írja ki. Egy elakadt lekérdezés miatt nem kell a beszélgetés fölé
    /// hibaüzenetet tenni, a következő kör úgyis jön.
    /// </summary>
    async Task RefreshQuietlyAsync()
    {
        try
        {
            var messages = await Api.GetMessagesAsync(partnerId: PartnerId);

            if (messages.Count == Messages.Count)
                return;

            Replace(messages);

            await MarkIncomingReadAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Beszélgetés frissítése nem sikerült: {ex.Message}");
        }
    }

    [RelayCommand]
    async Task SendAsync()
    {
        if (string.IsNullOrWhiteSpace(Draft))
            return;

        var content = Draft.Trim();

        // A mezőt azonnal ürítjük: küldés közben a felhasználó már a
        // következőt gépelheti.
        Draft = string.Empty;

        var sent = await RunAsync(async () =>
        {
            var message = await Api.SendMessageAsync(new SendMessageRequest
            {
                ReceiverId = PartnerId,
                Content = content
            });

            // Azonosító szerint nézzük: a hub ugyanezt az üzenetet nekünk is
            // elküldi, és kétszer nem szabad megjelennie.
            if (!Messages.Any(m => m.Id == message.Id))
                Messages.Add(new ChatMessageItem(message, IsMine: true));
        });

        if (!sent)
            Draft = content;
    }
}
