using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vissza.Maui.Services;
using Vissza.Shared.Dtos;

namespace Vissza.Maui.ViewModels;

/// <summary>
/// Egy beszélgetés. A ChatScreen.js leképezése.
///
/// A frissítés időzítővel megy, ahogy a régi appban is. Ez ideiglenes:
/// az ASP.NET Core-ral a SignalR gyakorlatilag ingyen van, és valós idejűvé
/// tenné a beszélgetést - lásd MAUI_TERV.md 6.1.
/// </summary>
public sealed partial class ChatViewModel(IServiceProvider services, AuthService auth) : ViewModelBase
{
    /// <summary>
    /// Öt másodperc. A régi app is ilyen nagyságrenddel kérdezett; sűrűbben
    /// már a szervert terhelné anélkül, hogy érezhetően gyorsabb lenne.
    /// </summary>
    static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);

    IVisszaApi Api => services.GetRequiredService<IVisszaApi>();

    CancellationTokenSource? _polling;

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

    public void StartPolling()
    {
        StopPolling();

        _polling = new CancellationTokenSource();
        _ = PollAsync(_polling.Token);
    }

    public void StopPolling()
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
            // A képernyő elhagyása - nem hiba.
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

            Messages.Add(new ChatMessageItem(message, IsMine: true));
        });

        if (!sent)
            Draft = content;
    }
}
