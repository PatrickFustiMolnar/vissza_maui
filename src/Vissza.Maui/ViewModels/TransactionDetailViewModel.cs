using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vissza.Maui.Resources;
using Vissza.Maui.Services;
using Vissza.Shared.Dtos;
using Vissza.Shared.Enums;

namespace Vissza.Maui.ViewModels;

/// <summary>
/// Egy átvétel részletei és lezárása. A TransactionDetailScreen.js
/// leképezése.
///
/// Ez a képernyő zárja be a kört: itt erősíti meg mindkét fél az átvételt,
/// és itt lesz a felajánlásból lezárt ügy. A szabályokat a szerver őrzi -
/// a felület csak felkínálja azt, ami az adott félnek megengedett.
/// </summary>
public sealed partial class TransactionDetailViewModel(
    IServiceProvider services, AuthService auth) : ViewModelBase
{
    IVisszaApi Api => services.GetRequiredService<IVisszaApi>();

    [ObservableProperty]
    public partial int TransactionId { get; set; }

    [ObservableProperty]
    public partial TransactionDto? Transaction { get; set; }

    [ObservableProperty]
    public partial OfferDto? Offer { get; set; }

    [ObservableProperty]
    public partial string PartnerName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int PartnerId { get; set; }

    int CurrentUserId => auth.CurrentUser?.Id ?? 0;

    public bool IsDonor => Transaction?.DonorId == CurrentUserId;
    public bool IsCollector => Transaction?.CollectorId == CurrentUserId;

    public bool IsPending => Transaction?.Status == TransactionStatus.Pending;
    public bool IsCompleted => Transaction?.Status == TransactionStatus.Completed;

    /// <summary>Megerősítettem-e már a saját nevemben.</summary>
    public bool IConfirmed => Transaction is { } t && (IsDonor ? t.DonorConfirmed : t.CollectorConfirmed);

    public bool PartnerConfirmed => Transaction is { } t && (IsDonor ? t.CollectorConfirmed : t.DonorConfirmed);

    /// <summary>A megerősítés gombja csak addig kell, amíg nem tettem meg.</summary>
    public bool CanConfirm => IsPending && !IConfirmed;

    /// <summary>
    /// Megtettem a magamét, de a másik fél még nem. Csak ilyenkor van értelme
    /// a várakozásról írni: lezárás után vagy a partner megerősítése után a
    /// szöveg félrevezető lenne.
    /// </summary>
    public bool IsWaitingForPartner => IsPending && IConfirmed && !PartnerConfirmed;

    /// <summary>
    /// Lezárni csak akkor lehet, ha mindkét fél megerősített. A szerver
    /// ugyanezt kikényszeríti - a gomb elrejtése csak azt előzi meg, hogy a
    /// felhasználó hibaüzenetbe fusson.
    /// </summary>
    public bool CanComplete => IsPending && Transaction is { DonorConfirmed: true, CollectorConfirmed: true };

    public bool CanCancel => IsPending;

    public string StatusText => Transaction?.Status switch
    {
        TransactionStatus.Completed => "Lezárt átvétel",
        TransactionStatus.Cancelled => "Visszavont átvétel",
        _ => "Átvétel folyamatban"
    };

    public string ConfirmationText => Transaction is not { } t
        ? string.Empty
        : $"Felajánló: {(t.DonorConfirmed ? "megerősítette" : "még nem erősítette meg")}\n"
          + $"Gyűjtő: {(t.CollectorConfirmed ? "megerősítette" : "még nem erősítette meg")}";

    public string SummaryText => Transaction is not { } t
        ? string.Empty
        : $"{t.Quantity} db {DomainLabels.BottleTypeShort(t.BottleType)} · {t.Location}";

    static readonly string[] Derived =
    [
        nameof(IsDonor), nameof(IsCollector), nameof(IsPending), nameof(IsCompleted),
        nameof(IConfirmed), nameof(PartnerConfirmed), nameof(CanConfirm),
        nameof(IsWaitingForPartner), nameof(CanComplete), nameof(CanCancel), nameof(StatusText),
        nameof(ConfirmationText), nameof(SummaryText)
    ];

    partial void OnTransactionChanged(TransactionDto? value)
    {
        foreach (var name in Derived)
            OnPropertyChanged(name);
    }

    public async Task LoadAsync()
    {
        await RunAsync(async () =>
        {
            var transaction = await Api.GetTransactionAsync(TransactionId);

            Transaction = transaction;
            Offer = await Api.GetOfferAsync(transaction.OfferId);

            // A partner az, aki nem én vagyok. A nevét a felajánlásból
            // vesszük, mert az API a tranzakcióban csak azonosítót ad.
            var donor = transaction.DonorId == CurrentUserId;

            PartnerId = donor ? transaction.CollectorId : transaction.DonorId;
            PartnerName = (donor ? Offer?.SelectedCollectorName : Offer?.DonorName) ?? "Partner";
        });
    }

    [RelayCommand]
    async Task ConfirmAsync()
    {
        if (Transaction is null)
            return;

        // Mindenki csak a saját nevében erősít meg; a szerver a másik fél
        // mezőjét figyelmen kívül hagyná.
        var request = IsDonor
            ? new UpdateTransactionRequest { DonorConfirmed = true }
            : new UpdateTransactionRequest { CollectorConfirmed = true };

        await RunAsync(async () => Transaction = await Api.UpdateTransactionAsync(TransactionId, request));
    }

    [RelayCommand]
    async Task CompleteAsync()
    {
        if (Transaction is null)
            return;

        var confirmed = await Shell.Current.DisplayAlertAsync(
            "Átvétel lezárása",
            "Ezzel az átvétel véglegesen lezárul, és mindkét fél statisztikája nő.",
            "Lezárás", "Mégsem");

        if (!confirmed)
            return;

        var done = await RunAsync(async () => Transaction = await Api.UpdateTransactionAsync(
            TransactionId, new UpdateTransactionRequest { Status = TransactionStatus.Completed }));

        if (done)
            await Shell.Current.DisplayAlertAsync("Kész", "Az átvétel lezárult.", "Rendben");
    }

    [RelayCommand]
    async Task CancelAsync()
    {
        var confirmed = await Shell.Current.DisplayAlertAsync(
            "Átvétel visszavonása",
            "A felajánlás ezzel újra elérhetővé válik.",
            "Visszavonás", "Mégsem");

        if (!confirmed)
            return;

        await RunAsync(async () => Transaction = await Api.UpdateTransactionAsync(
            TransactionId, new UpdateTransactionRequest { Status = TransactionStatus.Cancelled }));
    }

    /// <summary>Értékelni csak lezárt átvétel után lehet.</summary>
    [RelayCommand]
    async Task RateAsync()
    {
        if (!IsCompleted || PartnerId == 0)
            return;

        await Shell.Current.GoToAsync(
            $"rating?transactionId={TransactionId}&ratedId={PartnerId}"
            + $"&ratedName={Uri.EscapeDataString(PartnerName)}");
    }

    [RelayCommand]
    async Task OpenChatAsync()
    {
        if (PartnerId == 0)
            return;

        await Shell.Current.GoToAsync(
            $"chat?partnerId={PartnerId}&partnerName={Uri.EscapeDataString(PartnerName)}");
    }
}
