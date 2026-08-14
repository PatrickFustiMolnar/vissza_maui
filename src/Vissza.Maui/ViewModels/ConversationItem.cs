using System.Globalization;
using Vissza.Maui.Resources;
using Vissza.Maui.Services;
using Vissza.Shared.Dtos;

namespace Vissza.Maui.ViewModels;

/// <summary>
/// Egy beszélgetés a listában. A monogramot és az időpontot itt számoljuk,
/// nem a XAML-ben - ugyanaz a szabály érvényes rá, mint máshol az appban.
/// </summary>
public sealed record ConversationItem(ConversationDto Conversation)
{
    public UserSummaryDto? Partner => Conversation.Partner;

    public string PartnerName => Conversation.Partner?.Name ?? "Ismeretlen";

    public string Initials => DomainLabels.Initials(Conversation.Partner?.Name);

    public string? PartnerImage => Conversation.Partner?.ProfileImage;

    public bool HasPartnerImage => !string.IsNullOrWhiteSpace(Conversation.Partner?.ProfileImage);

    public string LastMessage => Conversation.LastMessage?.Content ?? string.Empty;

    public int UnreadCount => Conversation.UnreadCount;

    public bool HasUnread => Conversation.UnreadCount > 0;

    /// <summary>
    /// Mai üzenetnél óra:perc, régebbinél dátum. Egy tegnapi beszélgetésnél a
    /// puszta időpont félrevezető lenne.
    /// </summary>
    public string TimeText
    {
        get
        {
            if (Conversation.LastMessageAt is not { } at)
                return string.Empty;

            var local = Times.ToLocal(at);

            return local.Date == DateTime.Today
                ? local.ToString("HH:mm", CultureInfo.CurrentCulture)
                : local.ToString("MM. dd.", CultureInfo.CurrentCulture);
        }
    }

    /// <summary>A kereséshez: névben és az utolsó üzenetben is keresünk.</summary>
    public bool Matches(string query) =>
        PartnerName.Contains(query, StringComparison.CurrentCultureIgnoreCase)
        || LastMessage.Contains(query, StringComparison.CurrentCultureIgnoreCase);
}
