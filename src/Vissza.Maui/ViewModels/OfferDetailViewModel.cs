using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vissza.Maui.Resources;
using Vissza.Maui.Services;
using Vissza.Shared.Dtos;
using Vissza.Shared.Enums;

namespace Vissza.Maui.ViewModels;

/// <summary>
/// Egy felajánlás teljes lapja. Az OfferDetailScreen.js leképezése.
///
/// Innen indul a jelentkezés, és innen nyílik a beszélgetés is, ha az
/// átvétel már folyamatban van. A listakártya csak a lényeget mutatja;
/// a fénykép, az elérhetőségi idő és a megjegyzés csak itt látszik.
/// </summary>
public sealed partial class OfferDetailViewModel(
    IServiceProvider services, AuthService auth) : ViewModelBase
{
    IVisszaApi Api => services.GetRequiredService<IVisszaApi>();

    /// <summary>A saját jelentkezésem erre a felajánlásra, ha van.</summary>
    CollectionRequestDto? _myRequest;

    int CurrentUserId => auth.CurrentUser?.Id ?? 0;

    [ObservableProperty]
    public partial int OfferId { get; set; }

    [ObservableProperty]
    public partial OfferDto? Offer { get; set; }

    [ObservableProperty]
    public partial string RequestMessage { get; set; } = string.Empty;

    // --- fejléc ---

    public string? PhotoUrl => Offer?.PhotoUrl;
    public bool HasPhoto => !string.IsNullOrWhiteSpace(Offer?.PhotoUrl);

    public string QuantityText => Offer?.Quantity.ToString(CultureInfo.CurrentCulture) ?? string.Empty;

    public string EstimatedValueText =>
        Offer is null ? string.Empty : $"~{DomainLabels.EstimatedValue(Offer.Quantity):N0} Ft visszaváltási érték";

    public string BottleTypeKind =>
        Offer is null ? "neutral" : DomainLabels.BottleTypeBadgeKind(Offer.BottleType);

    public string BottleTypeLabel =>
        Offer is null ? string.Empty : DomainLabels.BottleType(Offer.BottleType);

    /// <summary>Az aktív állapot magától értetődő, ezért csak a másik kettőt jelezzük.</summary>
    public bool ShowStatusBadge => Offer is not null && Offer.Status != OfferStatus.Active;

    public string StatusKind =>
        Offer is null ? "neutral" : DomainLabels.StatusBadgeKind(Offer.Status);

    public string StatusLabel =>
        Offer is null ? string.Empty : DomainLabels.OfferStatus(Offer.Status);

    public bool ShowOtherDescription =>
        Offer?.BottleType == BottleType.Other && !string.IsNullOrWhiteSpace(Offer.OtherDescription);

    public string? OtherDescription => Offer?.OtherDescription;

    // --- helyszín, idő, megjegyzés ---

    public string Address => Offer?.Address ?? string.Empty;

    public bool HasAvailability => Offer is { AvailableFrom: not null } or { AvailableUntil: not null };

    public string AvailabilityText
    {
        get
        {
            if (Offer is not { } offer)
                return string.Empty;

            var lines = new List<string>();

            if (offer.AvailableFrom is { } from)
                lines.Add($"Elérhető innen: {Format(from)}");

            if (offer.AvailableUntil is { } until)
                lines.Add($"Elérhető eddig: {Format(until)}");

            return string.Join('\n', lines);
        }
    }

    static string Format(DateTime value) =>
        Times.ToLocal(value).ToString("yyyy. MM. dd. HH:mm", CultureInfo.CurrentCulture);

    public bool HasNotes => !string.IsNullOrWhiteSpace(Offer?.Notes);
    public string? Notes => Offer?.Notes;

    // --- felajánló és gyűjtő ---

    public string DonorName => Offer?.DonorName ?? "Ismeretlen";
    public string DonorInitials => DomainLabels.Initials(Offer?.DonorName);
    public string? DonorImage => Offer?.DonorProfileImage;
    public bool HasDonorImage => !string.IsNullOrWhiteSpace(Offer?.DonorProfileImage);
    public bool HasDonorRating => Offer?.DonorRating is > 0;
    public string DonorRatingText => Rating(Offer?.DonorRating);

    public bool HasCollector => !string.IsNullOrWhiteSpace(Offer?.SelectedCollectorName);
    public string CollectorName => Offer?.SelectedCollectorName ?? string.Empty;
    public string CollectorInitials => DomainLabels.Initials(Offer?.SelectedCollectorName);
    public string? CollectorImage => Offer?.SelectedCollectorProfileImage;
    public bool HasCollectorImage => !string.IsNullOrWhiteSpace(Offer?.SelectedCollectorProfileImage);
    public bool HasCollectorRating => Offer?.SelectedCollectorRating is > 0;
    public string CollectorRatingText => Rating(Offer?.SelectedCollectorRating);

    // A 0,00 átlag azt jelenti, hogy még senki nem értékelte - ilyenkor a
    // csillagot elhagyjuk, ahogy a listakártyán is.
    static string Rating(decimal? value) =>
        value is { } rating ? $"★ {rating.ToString("0.0", CultureInfo.CurrentCulture)}" : string.Empty;

    // --- szerepek és műveletek ---

    public bool IsDonor => Offer?.DonorId == CurrentUserId;

    /// <summary>
    /// Jelentkezni aktív felajánlásra lehet, a sajátunkra nem, és csak akkor,
    /// ha nincs élő jelentkezésünk. A visszavont nem számít élőnek: azt a
    /// szerver újraéleszti egy új jelentkezéssel.
    /// </summary>
    public bool CanRequest =>
        Offer is { Status: OfferStatus.Active }
        && !IsDonor
        && CurrentUserId != 0
        && _myRequest is null or { Status: RequestStatus.Cancelled };

    public bool HasRequestState => _myRequest is not null and not { Status: RequestStatus.Cancelled };

    public string RequestStateText => _myRequest?.Status switch
    {
        RequestStatus.Pending => "Elküldted az érdeklődésed. A felajánló még nem döntött.",
        RequestStatus.Accepted => "A felajánló elfogadta a jelentkezésed.",
        RequestStatus.Rejected => "A felajánló mást választott erre a felajánlásra.",
        _ => string.Empty
    };

    /// <summary>Visszavonni csak a még el nem bírált jelentkezést van értelme.</summary>
    public bool CanWithdraw => _myRequest is { Status: RequestStatus.Pending };

    /// <summary>
    /// Beszélgetni akkor lehet, ha az átvétel folyamatban van, és a másik fél
    /// ismert - tehát a felajánló és a kiválasztott gyűjtő között.
    /// </summary>
    public bool CanChat =>
        Offer is { Status: OfferStatus.Reserved }
        && (IsDonor ? Offer.SelectedCollectorId is not null : Offer.SelectedCollectorId == CurrentUserId);

    static readonly string[] Derived =
    [
        nameof(PhotoUrl), nameof(HasPhoto), nameof(QuantityText), nameof(EstimatedValueText),
        nameof(BottleTypeKind), nameof(BottleTypeLabel), nameof(ShowStatusBadge),
        nameof(StatusKind), nameof(StatusLabel), nameof(ShowOtherDescription), nameof(OtherDescription),
        nameof(Address), nameof(HasAvailability), nameof(AvailabilityText),
        nameof(HasNotes), nameof(Notes),
        nameof(DonorName), nameof(DonorInitials), nameof(DonorImage), nameof(HasDonorImage),
        nameof(HasDonorRating), nameof(DonorRatingText),
        nameof(HasCollector), nameof(CollectorName), nameof(CollectorInitials), nameof(CollectorImage),
        nameof(HasCollectorImage), nameof(HasCollectorRating), nameof(CollectorRatingText),
        nameof(IsDonor), nameof(CanRequest), nameof(HasRequestState), nameof(RequestStateText),
        nameof(CanWithdraw), nameof(CanChat)
    ];

    void RefreshDerived()
    {
        foreach (var name in Derived)
            OnPropertyChanged(name);
    }

    partial void OnOfferChanged(OfferDto? value) => RefreshDerived();

    public async Task LoadAsync()
    {
        await RunAsync(async () =>
        {
            var offerTask = Api.GetOfferAsync(OfferId);

            // A saját jelentkezésem. A végpont amúgy is csak azt adná vissza,
            // ami rám tartozik, de a szűrő egy kört megspórol.
            var requestTask = CurrentUserId != 0
                ? Api.GetCollectionRequestsAsync(offerId: OfferId, collectorId: CurrentUserId)
                : Task.FromResult<IReadOnlyList<CollectionRequestDto>>([]);

            await Task.WhenAll(offerTask, requestTask);

            _myRequest = (await requestTask).FirstOrDefault();
            Offer = await offerTask;

            RefreshDerived();
        });
    }

    [RelayCommand]
    async Task SendRequestAsync()
    {
        if (Offer is not { } offer)
            return;

        var request = new CreateCollectionRequestRequest
        {
            OfferId = offer.Id,
            Message = string.IsNullOrWhiteSpace(RequestMessage) ? null : RequestMessage.Trim()
        };

        if (!await RunAsync(() => Api.CreateCollectionRequestAsync(request)))
            return;

        RequestMessage = string.Empty;

        await LoadAsync();
    }

    [RelayCommand]
    async Task WithdrawAsync()
    {
        if (_myRequest is not { } request)
            return;

        var confirmed = await Shell.Current.DisplayAlertAsync(
            "Érdeklődés visszavonása",
            "Visszavonod a jelentkezésed erre a felajánlásra?",
            "Visszavonás", "Mégsem");

        if (!confirmed)
            return;

        var updated = await RunAsync(() => Api.UpdateCollectionRequestAsync(
            request.Id, new UpdateCollectionRequestRequest { Status = RequestStatus.Cancelled }));

        if (updated)
            await LoadAsync();
    }

    [RelayCommand]
    async Task OpenChatAsync()
    {
        if (Offer is not { } offer || !CanChat)
            return;

        var (partnerId, partnerName) = IsDonor
            ? (offer.SelectedCollectorId, offer.SelectedCollectorName)
            : (offer.DonorId, offer.DonorName);

        if (partnerId is not { } id)
            return;

        await Shell.Current.GoToAsync(
            $"chat?partnerId={id}&partnerName={Uri.EscapeDataString(partnerName ?? "Partner")}");
    }
}
