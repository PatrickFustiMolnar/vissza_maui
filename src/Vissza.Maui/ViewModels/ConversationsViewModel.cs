using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vissza.Maui.Services;
using Vissza.Shared.Dtos;

namespace Vissza.Maui.ViewModels;

/// <summary>
/// Beszélgetéslista. A ConversationsScreen.js leképezése.
/// </summary>
public sealed partial class ConversationsViewModel(
    IServiceProvider services, ChatHubService hub) : ViewModelBase
{
    IVisszaApi Api => services.GetRequiredService<IVisszaApi>();

    bool _subscribed;

    /// <summary>
    /// Élő frissítés: a lista a megnyitáskor amúgy is betölt, ez arra kell,
    /// hogy nyitva hagyott listánál se kelljen visszalépni ahhoz, hogy egy új
    /// üzenet és az olvasatlan szám megjelenjen.
    /// </summary>
    public async Task ListenAsync()
    {
        if (!_subscribed)
        {
            hub.MessageReceived += OnHubMessage;
            _subscribed = true;
        }

        await hub.StartAsync();
    }

    public void StopListening()
    {
        if (!_subscribed)
            return;

        hub.MessageReceived -= OnHubMessage;
        _subscribed = false;
    }

    void OnHubMessage(object? sender, ChatMessageDto message) =>
        MainThread.BeginInvokeOnMainThread(async () => await LoadAsync());

    IReadOnlyList<ConversationItem> _loaded = [];

    public ObservableCollection<ConversationItem> Conversations { get; } = [];

    [ObservableProperty]
    public partial int UnreadCount { get; set; }

    public bool HasUnread => UnreadCount > 0;

    partial void OnUnreadCountChanged(int value) => OnPropertyChanged(nameof(HasUnread));

    /// <summary>
    /// Keresés a listában. Kliensoldalon szűr: a beszélgetések száma
    /// nagyságrendekkel kisebb, mint amiért érdemes lenne a szervert
    /// megkérdezni minden leütésnél.
    /// </summary>
    [ObservableProperty]
    public partial string SearchQuery { get; set; } = string.Empty;

    partial void OnSearchQueryChanged(string value) => ApplySearch();

    void ApplySearch()
    {
        var query = SearchQuery.Trim();

        var items = query.Length == 0
            ? _loaded
            : [.. _loaded.Where(item => item.Matches(query))];

        Conversations.Clear();

        foreach (var item in items)
            Conversations.Add(item);
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        await RunAsync(async () =>
        {
            // A beszélgetések és az olvasatlan szám egyszerre indul: a
            // fejlécnek és a listának is friss adat kell.
            var conversationsTask = Api.GetConversationsAsync();
            var unreadTask = Api.GetUnreadCountAsync();

            await Task.WhenAll(conversationsTask, unreadTask);

            _loaded = [.. (await conversationsTask).Select(c => new ConversationItem(c))];

            ApplySearch();

            UnreadCount = (await unreadTask).Count;
        });
    }

    [RelayCommand]
    static async Task OpenAsync(ConversationItem? item)
    {
        if (item?.Partner is not { } partner)
            return;

        await Shell.Current.GoToAsync($"chat?partnerId={partner.Id}&partnerName={Uri.EscapeDataString(partner.Name)}");
    }
}
