using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vissza.Maui.Maps;
using Vissza.Maui.Resources;
using Vissza.Maui.Services;
using Vissza.Shared.Dtos;

namespace Vissza.Maui.ViewModels;

/// <summary>
/// A főoldal: térkép a közeli felajánlásokkal és visszaváltó helyekkel.
/// A DashboardScreen.js leképezése.
/// </summary>
public sealed partial class DashboardViewModel(IServiceProvider services, AuthService auth) : ViewModelBase
{
    IVisszaApi Api => services.GetRequiredService<IVisszaApi>();

    IReadOnlyList<OfferDto> _offers = [];
    IReadOnlyList<ReturnLocationDto> _locations = [];

    [ObservableProperty]
    public partial string Greeting { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool ShowOffers { get; set; } = true;

    [ObservableProperty]
    public partial bool ShowReturnLocations { get; set; } = true;

    [ObservableProperty]
    public partial bool HasSelection { get; set; }

    [ObservableProperty]
    public partial string SelectionTitle { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SelectionSubtitle { get; set; } = string.Empty;

    /// <summary>A nézet ezen keresztül kapja meg a kirakandó tűket.</summary>
    public event EventHandler<IReadOnlyList<MapPin>>? PinsChanged;

    /// <summary>A nézet ezen keresztül kapja meg, hova álljon a kamera.</summary>
    public event EventHandler<(double Lat, double Lng)>? CenterRequested;

    partial void OnShowOffersChanged(bool value) => PublishPins();

    partial void OnShowReturnLocationsChanged(bool value) => PublishPins();

    [RelayCommand]
    public async Task LoadAsync()
    {
        Greeting = auth.CurrentUser is { } user ? $"Szia, {user.Name}!" : string.Empty;

        await RunAsync(async () =>
        {
            // Egyszerre indul a kettő: a régi képernyő is Promise.all-lal
            // kérte le őket, és így a lassabbik szabja meg a várakozást.
            var offersTask = Api.GetOffersAsync(status: "active");
            var locationsTask = Api.GetReturnLocationsAsync();

            await Task.WhenAll(offersTask, locationsTask);

            // Koordináta nélküli elemek kiszűrése: ezek a térképen a 0,0
            // ponton, a Guineai-öbölben jelennének meg.
            _offers = [.. (await offersTask).Where(o => o.LocationLat != 0 && o.LocationLng != 0)];
            _locations = [.. (await locationsTask).Where(l => l.Lat != 0 && l.Lng != 0)];

            PublishPins();
        });

        await CenterOnUserAsync();
    }

    /// <summary>
    /// A helymeghatározás nem blokkolja a listák betöltését, és a hibája sem
    /// üzenet: ha nincs engedély vagy nincs jel, a térkép marad ott, ahol van.
    /// </summary>
    async Task CenterOnUserAsync()
    {
        try
        {
            var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

            if (status != PermissionStatus.Granted)
                return;

            var location = await Geolocation.GetLastKnownLocationAsync()
                ?? await Geolocation.GetLocationAsync(
                    new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10)));

            if (location is null)
                return;

            _userLocation = (location.Latitude, location.Longitude);

            PublishPins();
            CenterRequested?.Invoke(this, _userLocation.Value);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Helymeghatározás nem elérhető: {ex.Message}");
        }
    }

    (double Lat, double Lng)? _userLocation;

    void PublishPins()
    {
        var pins = new List<MapPin>();

        if (_userLocation is { } user)
        {
            pins.Add(new MapPin
            {
                Kind = MapPinKind.User,
                Latitude = user.Lat,
                Longitude = user.Lng,
                Title = "Te itt vagy"
            });
        }

        // A visszaváltók előbb kerülnek a listába, hogy a felajánlások
        // rajzolódjanak föléjük - egy helyen álló tűk közül a felajánlás
        // a lényegesebb.
        if (ShowReturnLocations)
        {
            pins.AddRange(_locations.Select(location => new MapPin
            {
                Kind = MapPinKind.ReturnLocation,
                Latitude = (double)location.Lat,
                Longitude = (double)location.Lng,
                Title = location.Name,
                Subtitle = location.Address,
                Payload = location
            }));
        }

        if (ShowOffers)
        {
            pins.AddRange(_offers.Select(offer => new MapPin
            {
                Kind = MapPinKind.Offer,
                Latitude = (double)offer.LocationLat,
                Longitude = (double)offer.LocationLng,
                Title = $"{offer.Quantity} db {DomainLabels.BottleTypeShort(offer.BottleType)}",
                Subtitle = offer.Address,
                Payload = offer
            }));
        }

        PinsChanged?.Invoke(this, pins);
    }

    /// <summary>
    /// A kiválasztott felajánlás azonosítója, ha a tű felajánlást jelöl.
    /// Visszaváltó helyre nincs részletlap, ezért ott null.
    /// </summary>
    [ObservableProperty]
    public partial int? SelectedOfferId { get; set; }

    public bool CanOpenOffer => SelectedOfferId is not null;

    partial void OnSelectedOfferIdChanged(int? value) => OnPropertyChanged(nameof(CanOpenOffer));

    public void Select(MapPin pin)
    {
        SelectionTitle = pin.Title;
        SelectedOfferId = pin.Payload is OfferDto selected ? selected.Id : null;

        SelectionSubtitle = pin.Payload switch
        {
            OfferDto offer =>
                $"{offer.Address}\nFelajánló: {offer.DonorName}"
                + $"\nBecsült érték: ~{DomainLabels.EstimatedValue(offer.Quantity):N0} Ft",

            ReturnLocationDto location =>
                $"{location.Address}\n{location.OpeningHours ?? "Nyitvatartás nincs megadva"}",

            _ => pin.Subtitle ?? string.Empty
        };

        HasSelection = true;
    }

    [RelayCommand]
    void ClearSelection() => HasSelection = false;

    [RelayCommand]
    async Task OpenOfferAsync()
    {
        if (SelectedOfferId is not { } id)
            return;

        // A panelt bezárjuk: visszatéréskor a térkép ne egy régi kijelöléssel
        // fogadjon, ami közben már el is kelhetett.
        HasSelection = false;

        await Shell.Current.GoToAsync($"offer?offerId={id}");
    }

    [RelayCommand]
    async Task SignOutAsync()
    {
        await auth.SignOutAsync();
        await Shell.Current.GoToAsync("//login");
    }
}
