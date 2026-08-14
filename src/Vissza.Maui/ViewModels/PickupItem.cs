using System.Globalization;
using Vissza.Maui.Resources;
using Vissza.Maui.Services;
using Vissza.Shared.Dtos;
using Vissza.Shared.Enums;

namespace Vissza.Maui.ViewModels;

/// <summary>
/// Egy átvétel a gyűjtő listájában. A tranzakcióból mindent kiolvasunk, ami
/// a sorhoz kell - a felajánlást nem kell külön lekérdezni hozzá.
/// </summary>
public sealed record PickupItem(TransactionDto Transaction)
{
    public int Id => Transaction.Id;

    public string Title =>
        $"{Transaction.Quantity} db {DomainLabels.BottleTypeShort(Transaction.BottleType)}";

    public string Location => Transaction.Location ?? string.Empty;

    public string EstimatedValueText =>
        $"~{DomainLabels.EstimatedValue(Transaction.Quantity):N0} Ft";

    public string CreatedAtText =>
        Times.ToLocal(Transaction.CreatedAt).ToString("yyyy. MM. dd.", CultureInfo.CurrentCulture);

    /// <summary>
    /// Mi a következő lépés. Függőben az számít, ki erősített már meg -
    /// a gyűjtő ebből látja, rajta van-e a sor.
    /// </summary>
    public string StatusText => Transaction switch
    {
        { Status: TransactionStatus.Completed } => "Lezárt átvétel",
        { Status: TransactionStatus.Cancelled } => "Visszavonva",
        { CollectorConfirmed: false } => "Rád vár a megerősítés",
        { DonorConfirmed: false } => "A felajánló megerősítésére vár",
        _ => "Mindkét fél megerősítette, lezárható"
    };

    public string StatusKind => Transaction.Status switch
    {
        TransactionStatus.Completed => "neutral",
        TransactionStatus.Cancelled => "neutral",
        _ => "reserved"
    };

    public string StatusBadge => Transaction.Status switch
    {
        TransactionStatus.Completed => "Lezárt",
        TransactionStatus.Cancelled => "Visszavont",
        _ => "Folyamatban"
    };
}
