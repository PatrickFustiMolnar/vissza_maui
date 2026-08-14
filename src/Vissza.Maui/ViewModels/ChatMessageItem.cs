using Vissza.Shared.Dtos;

namespace Vissza.Maui.ViewModels;

/// <summary>
/// Egy üzenet a beszélgetésben, azzal kiegészítve, hogy a saját oldalunkról
/// jött-e. Ezt a nézetmodell dönti el, mert ő ismeri a bejelentkezett
/// felhasználót - a XAML-ből egy konverter nem tudná paraméterként megkapni.
/// </summary>
public sealed record ChatMessageItem(ChatMessageDto Message, bool IsMine)
{
    public string Content => Message.Content;
    public DateTime CreatedAt => Message.CreatedAt;
    public int Id => Message.Id;
    public int ReceiverId => Message.ReceiverId;
    public bool IsRead => Message.IsRead;
}
