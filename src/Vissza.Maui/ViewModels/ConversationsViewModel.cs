using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vissza.Maui.Services;
using Vissza.Shared.Dtos;

namespace Vissza.Maui.ViewModels;

/// <summary>
/// Beszélgetéslista. A ConversationsScreen.js leképezése.
/// </summary>
public sealed partial class ConversationsViewModel(IServiceProvider services) : ViewModelBase
{
    IVisszaApi Api => services.GetRequiredService<IVisszaApi>();

    public ObservableCollection<ConversationDto> Conversations { get; } = [];

    [ObservableProperty]
    public partial int UnreadCount { get; set; }

    public bool HasUnread => UnreadCount > 0;

    partial void OnUnreadCountChanged(int value) => OnPropertyChanged(nameof(HasUnread));

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

            Conversations.Clear();

            foreach (var conversation in await conversationsTask)
                Conversations.Add(conversation);

            UnreadCount = (await unreadTask).Count;
        });
    }

    [RelayCommand]
    static async Task OpenAsync(ConversationDto? conversation)
    {
        if (conversation?.Partner is not { } partner)
            return;

        await Shell.Current.GoToAsync($"chat?partnerId={partner.Id}&partnerName={Uri.EscapeDataString(partner.Name)}");
    }
}
