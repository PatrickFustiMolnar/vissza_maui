using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vissza.Maui.Resources;
using Vissza.Maui.Services;
using Vissza.Shared.Dtos;
using Vissza.Shared.Enums;

namespace Vissza.Maui.ViewModels;

/// <summary>
/// Felajánlás: új létrehozása és a sajátok kezelése.
/// A GiveScreen.js leképezése.
/// </summary>
public sealed partial class GiveViewModel(
    IServiceProvider services,
    AuthService auth,
    GeocodingService geocoding,
    PhotoService photos) : ViewModelBase
{
    IVisszaApi Api => services.GetRequiredService<IVisszaApi>();

    public ObservableCollection<OfferDto> Offers { get; } = [];

    /// <summary>A kibontott felajánlásra érkezett, még el nem bírált jelentkezések.</summary>
    public ObservableCollection<CollectionRequestDto> PendingRequests { get; } = [];

    // --- listaszűrés ---

    public IReadOnlyList<string> StatusOptions { get; } = ["Aktív", "Folyamatban", "Lezárt"];

    [ObservableProperty]
    public partial int SelectedStatusIndex { get; set; }

    OfferStatus SelectedStatus => SelectedStatusIndex switch
    {
        1 => OfferStatus.Reserved,
        2 => OfferStatus.Completed,
        _ => OfferStatus.Active
    };

    partial void OnSelectedStatusIndexChanged(int value) => _ = LoadAsync();

    // --- űrlap ---

    [ObservableProperty]
    public partial bool IsFormVisible { get; set; }

    [ObservableProperty]
    public partial string Quantity { get; set; } = string.Empty;

    public IReadOnlyList<string> BottleTypeOptions { get; } =
        [.. Enum.GetValues<BottleType>().Select(DomainLabels.BottleType)];

    [ObservableProperty]
    public partial int SelectedBottleTypeIndex { get; set; }

    BottleType SelectedBottleType => Enum.GetValues<BottleType>()[SelectedBottleTypeIndex];

    public bool IsOtherSelected => SelectedBottleType == BottleType.Other;

    partial void OnSelectedBottleTypeIndexChanged(int value) => OnPropertyChanged(nameof(IsOtherSelected));

    [ObservableProperty]
    public partial string OtherDescription { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Address { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Notes { get; set; } = string.Empty;

    /// <summary>
    /// A feltöltött fotó relatív útvonala. Ezt küldjük az API-nak; a képernyő
    /// előnézete a PhotoPreview teljes URL-jét használja.
    /// </summary>
    [ObservableProperty]
    public partial string? PhotoPath { get; set; }

    public bool HasPhoto => !string.IsNullOrWhiteSpace(PhotoPath);

    /// <summary>
    /// A feltöltés relatív útvonalat ad vissza; a felajánlás létrehozásáig
    /// nincs szerveroldali kör, ami teljes URL-lé alakítaná - ezért itt
    /// alakítjuk.
    /// </summary>
    public string? PhotoPreview => ApiConfig.Absolute(PhotoPath);

    partial void OnPhotoPathChanged(string? value)
    {
        OnPropertyChanged(nameof(HasPhoto));
        OnPropertyChanged(nameof(PhotoPreview));
    }

    [ObservableProperty]
    public partial OfferDto? ExpandedOffer { get; set; }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (auth.CurrentUser is not { } user)
            return;

        await RunAsync(async () =>
        {
            var offers = await Api.GetOffersAsync(
                status: SelectedStatus.ToString().ToLowerInvariant(),
                donorId: user.Id);

            Offers.Clear();

            foreach (var offer in offers)
                Offers.Add(offer);
        });
    }

    [RelayCommand]
    void ToggleForm()
    {
        IsFormVisible = !IsFormVisible;
        ErrorMessage = null;
    }

    /// <summary>
    /// A fotó azonnal feltöltődik, nem a közzétételkor. Így a felhasználó
    /// rögtön látja, mit választott, és a felajánlás létrehozása egyetlen
    /// gyors kérés marad.
    /// </summary>
    [RelayCommand]
    async Task ChangePhotoAsync()
    {
        await RunAsync(async () =>
        {
            var result = await photos.ChooseAsync("Fotó a felajánláshoz", allowRemove: HasPhoto);

            PhotoPath = result.Choice switch
            {
                PhotoChoice.Uploaded => result.Path,
                PhotoChoice.Removed => null,
                _ => PhotoPath
            };
        });
    }

    /// <summary>
    /// A saját felajánlás teljes lapja. A felajánlónak is hasznos: itt látja
    /// a kiválasztott gyűjtőt az értékelésével együtt, és innen tud üzenni is.
    /// </summary>
    [RelayCommand]
    static async Task OpenDetailAsync(OfferDto? offer)
    {
        if (offer is null)
            return;

        await Shell.Current.GoToAsync($"offer?offerId={offer.Id}");
    }

    [RelayCommand]
    async Task SubmitAsync()
    {
        if (!int.TryParse(Quantity, out var quantity) || quantity <= 0
            || string.IsNullOrWhiteSpace(Address))
        {
            ErrorMessage = "Kérlek töltsd ki a kötelező mezőket";
            return;
        }

        // A cím koordinátáit a Nominatim adja. Ha nem sikerül, a felajánlás
        // nem jöhet létre: koordináta nélkül nem jelenne meg a térképen,
        // tehát senki nem találná meg.
        var coordinates = await geocoding.ResolveAsync(Address.Trim());

        if (coordinates is not { } position)
        {
            ErrorMessage = "Nem sikerült meghatározni a cím koordinátáit. "
                + "Kérlek ellenőrizd, hogy a cím helyes-e.";
            return;
        }

        var request = new CreateOfferRequest
        {
            Quantity = quantity,
            BottleType = SelectedBottleType,
            OtherDescription = IsOtherSelected && !string.IsNullOrWhiteSpace(OtherDescription)
                ? OtherDescription.Trim()
                : null,
            Address = Address.Trim(),
            LocationLat = (decimal)position.Lat,
            LocationLng = (decimal)position.Lng,
            Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim(),
            PhotoUrl = PhotoPath,
            Status = OfferStatus.Active
        };

        if (!await RunAsync(() => Api.CreateOfferAsync(request)))
            return;

        ResetForm();
        IsFormVisible = false;

        SelectedStatusIndex = 0;
        await LoadAsync();
    }

    void ResetForm()
    {
        Quantity = string.Empty;
        SelectedBottleTypeIndex = 0;
        OtherDescription = string.Empty;
        Address = string.Empty;
        Notes = string.Empty;
        PhotoPath = null;
    }

    [RelayCommand]
    async Task DeleteAsync(OfferDto? offer)
    {
        if (offer is null)
            return;

        var confirmed = await Shell.Current.DisplayAlertAsync(
            "Felajánlás visszavonása",
            $"Biztosan visszavonod? {offer.Quantity} db · {offer.Address}",
            "Visszavonás", "Mégsem");

        if (!confirmed)
            return;

        if (await RunAsync(() => Api.DeleteOfferAsync(offer.Id)))
            Offers.Remove(offer);
    }

    /// <summary>
    /// Egy felajánlás megnyitása. Aktív állapotban a jelentkezőket mutatja;
    /// ha viszont már le van foglalva vagy lezárt, akkor nincs kit
    /// elbírálni - ott az átvétel a következő lépés.
    /// </summary>
    [RelayCommand]
    async Task ToggleRequestsAsync(OfferDto? offer)
    {
        if (offer is null)
            return;

        if (offer.Status is not OfferStatus.Active)
        {
            await OpenTransactionAsync(offer);
            return;
        }

        if (ExpandedOffer?.Id == offer.Id)
        {
            ExpandedOffer = null;
            PendingRequests.Clear();
            return;
        }

        ExpandedOffer = offer;
        PendingRequests.Clear();

        await RunAsync(async () =>
        {
            var requests = await Api.GetCollectionRequestsAsync(
                offerId: offer.Id, status: "pending");

            foreach (var request in requests)
                PendingRequests.Add(request);
        });
    }

    /// <summary>
    /// A felajánláshoz tartozó átvétel megnyitása. Egy felajánlásnak
    /// legfeljebb egy nyitott átvétele van, ezért az elsőt vesszük.
    /// </summary>
    async Task OpenTransactionAsync(OfferDto offer)
    {
        TransactionDto? transaction = null;

        var found = await RunAsync(async () =>
        {
            var transactions = await Api.GetTransactionsAsync(offerId: offer.Id);
            transaction = transactions.FirstOrDefault();
        });

        if (!found)
            return;

        if (transaction is null)
        {
            ErrorMessage = "Ehhez a felajánláshoz nem tartozik átvétel.";
            return;
        }

        await Shell.Current.GoToAsync($"transaction?transactionId={transaction.Id}");
    }

    /// <summary>
    /// Elfogadás. A szerveren négy írást indít egyetlen tranzakcióban:
    /// lefoglalja a felajánlást, elutasítja a riválisokat és megnyitja
    /// az átvételt.
    /// </summary>
    [RelayCommand]
    Task AcceptAsync(CollectionRequestDto? request) => DecideAsync(request, RequestStatus.Accepted);

    [RelayCommand]
    Task RejectAsync(CollectionRequestDto? request) => DecideAsync(request, RequestStatus.Rejected);

    async Task DecideAsync(CollectionRequestDto? request, RequestStatus status)
    {
        if (request is null)
            return;

        var updated = await RunAsync(() => Api.UpdateCollectionRequestAsync(
            request.Id, new UpdateCollectionRequestRequest { Status = status }));

        if (!updated)
            return;

        PendingRequests.Remove(request);

        // Az elfogadás lefoglalja a felajánlást, tehát az kikerül az aktív
        // listából - ezért töltjük újra.
        if (status == RequestStatus.Accepted)
        {
            ExpandedOffer = null;
            PendingRequests.Clear();
            await LoadAsync();
        }
    }
}
