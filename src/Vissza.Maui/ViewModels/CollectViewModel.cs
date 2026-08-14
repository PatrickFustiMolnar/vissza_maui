using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vissza.Maui.Maps;
using Vissza.Maui.Resources;
using Vissza.Maui.Services;
using Vissza.Shared.Dtos;
using Vissza.Shared.Enums;

namespace Vissza.Maui.ViewModels;

/// <summary>
/// Gyűjtés: a közeli felajánlások böngészése és jelentkezés rájuk.
/// A CollectScreen.js leképezése.
///
/// Itt lett kész a régi app négy szűrőjéből az utolsó kettő. A palacktípus
/// és a minimális mennyiség szerveroldalon szűr; a távolság és a rendezés
/// viszont a felhasználó helyzetét igényli, ami a készüléken van, ezért
/// azok itt futnak.
/// </summary>
public sealed partial class CollectViewModel(IServiceProvider services, AuthService auth) : ViewModelBase
{
    IVisszaApi Api => services.GetRequiredService<IVisszaApi>();

    IReadOnlyList<OfferDto> _loaded = [];
    (double Lat, double Lng)? _position;

    public ObservableCollection<OfferDto> Offers { get; } = [];

    /// <summary>A saját átvételeim - folyamatban vagy lezárva.</summary>
    public ObservableCollection<PickupItem> Pickups { get; } = [];

    /// <summary>A megjelenített elemek távolsága, azonosító szerint.</summary>
    public Dictionary<int, double> DistancesKm { get; } = [];

    // --- nézetváltó ---
    //
    // Három nézet, ahogy a régi appban: elérhető felajánlások, a saját
    // folyamatban lévő átvételeim, és a lezártak. A második kettő nélkül a
    // gyűjtői oldal a jelentkezés elküldése után megszakadt: nem volt út a
    // saját átvételhez, tehát megerősíteni, lezárni és értékelni sem lehetett.

    [ObservableProperty]
    public partial int SelectedViewIndex { get; set; }

    public bool IsAvailableView => SelectedViewIndex == 0;
    public bool IsPendingView => SelectedViewIndex == 1;
    public bool IsCompletedView => SelectedViewIndex == 2;

    /// <summary>A térkép és a szűrők csak az elérhető felajánlásokhoz valók.</summary>
    public bool ShowSearchTools => IsAvailableView;

    public string EmptyText => SelectedViewIndex switch
    {
        1 => "Nincs folyamatban lévő átvételed.",
        2 => "Még nincs lezárt átvételed.",
        _ => "Nincs a szűrőknek megfelelő felajánlás."
    };

    partial void OnSelectedViewIndexChanged(int value)
    {
        foreach (var name in new[]
        {
            nameof(IsAvailableView), nameof(IsPendingView), nameof(IsCompletedView),
            nameof(ShowSearchTools), nameof(EmptyText)
        })
        {
            OnPropertyChanged(name);
        }

        _ = LoadAsync();
    }

    [RelayCommand]
    void SelectView(string? index)
    {
        if (int.TryParse(index, out var parsed))
            SelectedViewIndex = parsed;
    }

    public event EventHandler<IReadOnlyList<MapPin>>? PinsChanged;
    public event EventHandler<(double Lat, double Lng)>? CenterRequested;

    // --- szűrők ---

    public IReadOnlyList<string> BottleTypeOptions { get; } =
        ["Mind", .. Enum.GetValues<BottleType>().Select(DomainLabels.BottleType)];

    [ObservableProperty]
    public partial int SelectedBottleTypeIndex { get; set; }

    [ObservableProperty]
    public partial string MinQuantity { get; set; } = string.Empty;

    /// <summary>Kilométerben. Üresen nincs távolságkorlát.</summary>
    [ObservableProperty]
    public partial string MaxDistanceKm { get; set; } = "10";

    public IReadOnlyList<string> SortOptions { get; } = ["Távolság", "Mennyiség", "Legújabb"];

    [ObservableProperty]
    public partial int SelectedSortIndex { get; set; }

    partial void OnSelectedSortIndexChanged(int value) => ApplyLocalFilters();

    partial void OnMaxDistanceKmChanged(string value) => ApplyLocalFilters();

    // --- jelentkezés ---

    [ObservableProperty]
    public partial OfferDto? SelectedOffer { get; set; }

    [ObservableProperty]
    public partial string RequestMessage { get; set; } = string.Empty;

    public bool HasSelection => SelectedOffer is not null;

    partial void OnSelectedOfferChanged(OfferDto? value) => OnPropertyChanged(nameof(HasSelection));

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (!IsAvailableView)
        {
            await LoadPickupsAsync();
            return;
        }

        await ResolvePositionAsync();

        await RunAsync(async () =>
        {
            var bottleType = SelectedBottleTypeIndex > 0
                ? Enum.GetValues<BottleType>()[SelectedBottleTypeIndex - 1].ToString().ToLowerInvariant()
                : null;

            int? minQuantity = int.TryParse(MinQuantity, out var parsed) && parsed > 0 ? parsed : null;

            var offersTask = Api.GetOffersAsync(
                status: "active", bottleType: bottleType, minQuantity: minQuantity);

            // A saját jelentkezéseim: amire már jelentkeztem, azt nem
            // ajánljuk fel újra. A régi app is így szűrt.
            var requestsTask = auth.CurrentUser is { } user
                ? Api.GetCollectionRequestsAsync(collectorId: user.Id)
                : Task.FromResult<IReadOnlyList<CollectionRequestDto>>([]);

            await Task.WhenAll(offersTask, requestsTask);

            var applied = (await requestsTask)
                .Where(r => r.Status is RequestStatus.Pending or RequestStatus.Accepted)
                .Select(r => r.OfferId)
                .ToHashSet();

            _loaded = [.. (await offersTask)
                .Where(o => !applied.Contains(o.Id))
                .Where(o => o.DonorId != auth.CurrentUser?.Id)];

            ApplyLocalFilters();
        });
    }

    [RelayCommand]
    Task ApplyFiltersAsync() => LoadAsync();

    /// <summary>
    /// A saját átvételeim. Az elfogadott jelentkezésből a szerver azonnal
    /// átvételt nyit, ezért elég a tranzakciókat kérdezni - azok viszik a
    /// mennyiséget, a helyszínt és a megerősítések állását is.
    /// </summary>
    async Task LoadPickupsAsync()
    {
        if (auth.CurrentUser is not { } user)
            return;

        await RunAsync(async () =>
        {
            var status = IsCompletedView ? "completed" : "pending";

            var transactions = await Api.GetTransactionsAsync(
                collectorId: user.Id, status: status);

            Pickups.Clear();

            foreach (var transaction in transactions.OrderByDescending(t => t.CreatedAt))
                Pickups.Add(new PickupItem(transaction));
        });
    }

    /// <summary>Innen nyílik a megerősítés, a lezárás és az értékelés.</summary>
    [RelayCommand]
    static async Task OpenPickupAsync(PickupItem? pickup)
    {
        if (pickup is null)
            return;

        await Shell.Current.GoToAsync($"transaction?transactionId={pickup.Id}");
    }

    async Task ResolvePositionAsync()
    {
        if (_position is not null)
            return;

        try
        {
            if (await Permissions.RequestAsync<Permissions.LocationWhenInUse>() != PermissionStatus.Granted)
                return;

            var location = await Geolocation.GetLastKnownLocationAsync()
                ?? await Geolocation.GetLocationAsync(
                    new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10)));

            if (location is null)
                return;

            _position = (location.Latitude, location.Longitude);
            CenterRequested?.Invoke(this, _position.Value);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Helymeghatározás nem elérhető: {ex.Message}");
        }
    }

    /// <summary>
    /// Távolságszűrés és rendezés. Helyzet nélkül a távolság-alapú szűrő és
    /// rendezés kimarad - hamis számokat mutatni rosszabb, mint semmit.
    /// </summary>
    void ApplyLocalFilters()
    {
        var items = _loaded.AsEnumerable();

        DistancesKm.Clear();

        if (_position is { } position)
        {
            foreach (var offer in _loaded)
            {
                DistancesKm[offer.Id] = GeoDistance.Kilometers(
                    position.Lat, position.Lng,
                    (double)offer.LocationLat, (double)offer.LocationLng);
            }

            if (double.TryParse(MaxDistanceKm, out var maxKm) && maxKm > 0)
                items = items.Where(o => DistancesKm[o.Id] <= maxKm);

            items = SelectedSortIndex switch
            {
                0 => items.OrderBy(o => DistancesKm[o.Id]),
                1 => items.OrderByDescending(o => o.Quantity),
                _ => items.OrderByDescending(o => o.CreatedAt)
            };
        }
        else
        {
            // Távolság nélkül a "Távolság" rendezés értelmetlen: essen vissza
            // a legújabbra, hogy a lista sorrendje ne legyen véletlenszerű.
            items = SelectedSortIndex == 1
                ? items.OrderByDescending(o => o.Quantity)
                : items.OrderByDescending(o => o.CreatedAt);
        }

        var result = items.ToList();

        Offers.Clear();

        foreach (var offer in result)
            Offers.Add(offer);

        PublishPins(result);
    }

    void PublishPins(IReadOnlyList<OfferDto> offers)
    {
        var pins = new List<MapPin>();

        if (_position is { } position)
        {
            pins.Add(new MapPin
            {
                Kind = MapPinKind.User,
                Latitude = position.Lat,
                Longitude = position.Lng,
                Title = "Te itt vagy"
            });
        }

        pins.AddRange(offers.Select(offer => new MapPin
        {
            Kind = MapPinKind.Offer,
            Latitude = (double)offer.LocationLat,
            Longitude = (double)offer.LocationLng,
            Title = $"{offer.Quantity} db {DomainLabels.BottleTypeShort(offer.BottleType)}",
            Subtitle = offer.Address,
            Payload = offer
        }));

        PinsChanged?.Invoke(this, pins);
    }

    public string DistanceLabel(OfferDto offer) =>
        DistancesKm.TryGetValue(offer.Id, out var km)
            ? $"{km:0.#} km"
            : string.Empty;

    [RelayCommand]
    void Select(OfferDto? offer)
    {
        SelectedOffer = offer;
        RequestMessage = string.Empty;
        ErrorMessage = null;
    }

    [RelayCommand]
    void CancelSelection() => SelectedOffer = null;

    /// <summary>
    /// A teljes lap: fénykép, elérhetőségi idő, megjegyzés, felajánló. A
    /// listakártya ezeket nem fér ki, a döntéshez viszont kellhetnek.
    /// </summary>
    [RelayCommand]
    static async Task OpenDetailAsync(OfferDto? offer)
    {
        if (offer is null)
            return;

        await Shell.Current.GoToAsync($"offer?offerId={offer.Id}");
    }

    [RelayCommand]
    async Task SendRequestAsync()
    {
        if (SelectedOffer is not { } offer)
            return;

        var request = new CreateCollectionRequestRequest
        {
            OfferId = offer.Id,
            Message = string.IsNullOrWhiteSpace(RequestMessage) ? null : RequestMessage.Trim()
        };

        // A duplikátumot a szerver is elutasítja ("Request already exists"),
        // az üzenete pedig megjelenik a felhasználónak - nem kell külön
        // előzetes ellenőrző kérés, ahogy a régi appban volt.
        if (!await RunAsync(() => Api.CreateCollectionRequestAsync(request)))
            return;

        SelectedOffer = null;
        RequestMessage = string.Empty;

        await LoadAsync();
    }
}
