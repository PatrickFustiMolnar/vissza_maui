using Mapsui;
using Mapsui.Layers;
using Mapsui.Nts;
using Mapsui.Projections;
using Mapsui.Tiling;
using NetTopologySuite.Geometries;
using MBrush = Mapsui.Styles.Brush;
using MColor = Mapsui.Styles.Color;
using MPen = Mapsui.Styles.Pen;
using MapsuiMap = Mapsui.Map;
using MStyle = Mapsui.Styles.IStyle;
using MSymbolStyle = Mapsui.Styles.SymbolStyle;
using MSymbolType = Mapsui.Styles.SymbolType;
using MVectorStyle = Mapsui.Styles.VectorStyle;
using NtsPoint = NetTopologySuite.Geometries.Point;

namespace MapSpike;

public partial class MainPage : ContentPage
{
    // Budapest, ha nincs helymeghatározás
    const double FallbackLat = 47.4979;
    const double FallbackLng = 19.0402;

    // A users.notification_radius alapértelmezése 5 km
    const double RadiusMeters = 5000;

    // A gömbi Mercator fél kerülete méterben - a fátyol poligonhoz
    const double MercatorExtent = 20037508.34;

    MemoryLayer? _veilLayer;

    // A Mapsui 5-ben a találatkeresés lusta: meg kell adni, mely rétegeken
    // keressen. A 4-es IsMapInfoLayer jelző már nincs.
    readonly List<ILayer> _tappableLayers = new();

    bool _darkMode;

    public MainPage()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    async void OnLoaded(object? sender, EventArgs e)
    {
        var (lat, lng, source) = await ResolveLocationAsync();
        BuildMap(lat, lng, source);
    }

    // 2. kritérium: valódi GPS, ha megy; ha nem, jelezzük hogy tartalék koordináta
    static async Task<(double Lat, double Lng, string Source)> ResolveLocationAsync()
    {
        try
        {
            var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            if (status == PermissionStatus.Granted)
            {
                var location = await Geolocation.GetLastKnownLocationAsync()
                    ?? await Geolocation.GetLocationAsync(
                        new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10)));

                if (location is not null)
                    return (location.Latitude, location.Longitude, "GPS");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Helymeghatározás nem elérhető: {ex.Message}");
        }

        return (FallbackLat, FallbackLng, "tartalék");
    }

    void BuildMap(double lat, double lng, string source)
    {
        var map = new MapsuiMap();

        // A rétegsorrend számít: a fátyol a csempék fölé, de a markerek alá kerül,
        // különben a markereket is sötétítené.
        map.Layers.Add(OpenStreetMap.CreateTileLayer("MapSpike/0.1 (terkep spike)"));

        _veilLayer = CreateVeilLayer();
        _veilLayer.Enabled = false;
        map.Layers.Add(_veilLayer);

        map.Layers.Add(CreateRadiusLayer(lat, lng, RadiusMeters));

        var offerLayer = CreateOfferLayer(lat, lng);
        var userLayer = CreateUserLayer(lat, lng);
        map.Layers.Add(offerLayer);
        map.Layers.Add(userLayer);

        _tappableLayers.Clear();
        _tappableLayers.Add(offerLayer);
        _tappableLayers.Add(userLayer);

        var center = ToMercator(lng, lat);

        // Buktató: a Navigator csak akkor mozdul, ha a viewportnak már van
        // mérete. Konstruktorban hívva a CenterOnAndZoomTo némán elvész -
        // nincs kivétel, csak a térkép a 0,0 ponton marad. Ezért a
        // ViewportInitialized eseményre kötjük, és utána leiratkozunk.
        void CenterOnce(object? s, EventArgs e)
        {
            map.ViewportInitialized -= CenterOnce;
            map.Navigator.CenterOnAndZoomTo(center, 12);
        }

        map.ViewportInitialized += CenterOnce;

        // NYITOTT PROBLÉMA - lásd MAUI_TERV.md 7.6.
        //
        // A csempék aszinkron érkeznek, és a beérkezésük nem rajzolja újra a
        // vásznat: a térkép üres marad az első felhasználói gesztusig. Ez a
        // kezelő elvben a helyes horog, de ÖNMAGÁBAN NEM ELÉG - sem a
        // ForceUpdate(), sem a Refresh() nem érvényteleníti a Skia felületet
        // iOS-en. Ugyanez az oka, hogy a sötét fátyol sem jelenik meg.
        //
        // Nem Mapsui-képesség hiánya: gesztus után minden hibátlanul
        // rajzolódik. A megoldás a 3. fázisra marad.
        map.DataChanged += OnMapDataChanged;

        MapView.Map = map;
        MapView.Info += OnMapInfo;

        StatusLabel.Text =
            $"WGS84: {lat:F5}, {lng:F5}  ({source})\n" +
            $"EPSG:3857: {center.X:F0}, {center.Y:F0}\n" +
            $"{DemoOffers.Length} marker, {RadiusMeters / 1000:F0} km sugár";
    }

    // A leggyakoribb Mapsui-hiba: a FromLonLat először hosszúságot vár, utána
    // szélességet. Felcserélve nem hibázik, csak rossz helyre teszi a pontot.
    // Ezért megy minden átváltás ezen az egy metóduson keresztül.
    static MPoint ToMercator(double lng, double lat)
    {
        var (x, y) = SphericalMercator.FromLonLat(lng, lat);
        return new MPoint(x, y);
    }

    // 3. kritérium: saját, színezett markerek - a valódi app OfferCard színeivel
    static MemoryLayer CreateOfferLayer(double lat, double lng)
    {
        var features = new List<IFeature>(DemoOffers.Length);

        foreach (var offer in DemoOffers)
        {
            var feature = new PointFeature(ToMercator(lng + offer.DLng, lat + offer.DLat));
            feature["title"] = offer.Title;
            feature["body"] = offer.Body;
            feature.Styles = new List<MStyle> { MarkerStyle(offer.Fill, 0.9) };
            features.Add(feature);
        }

        return new MemoryLayer("Felajánlások") { Features = features };
    }

    static MemoryLayer CreateUserLayer(double lat, double lng)
    {
        var feature = new PointFeature(ToMercator(lng, lat));
        feature["title"] = "Te itt vagy";
        feature["body"] = "A térkép középpontja a helymeghatározás szerint.";
        feature.Styles = new List<MStyle> { MarkerStyle(new MColor(55, 138, 221), 1.1) };

        return new MemoryLayer("Felhasználó") { Features = new[] { feature } };
    }

    static MSymbolStyle MarkerStyle(MColor fill, double scale) => new()
    {
        SymbolType = MSymbolType.Ellipse,
        SymbolScale = scale,
        Fill = new MBrush(fill),
        Outline = new MPen(MColor.White, 3)
    };

    // 5. kritérium: sugárkör. A Mapsuiban nincs beépített Circle, ezért NTS
    // Buffer()-rel csinálunk poligont.
    static MemoryLayer CreateRadiusLayer(double lat, double lng, double meters)
    {
        var center = ToMercator(lng, lat);

        // Mercatorban egy "méter" nem méter: a torzítás a szélesség koszinuszával
        // arányos. Korrekció nélkül a kör Budapesten ~1,5-szer akkora lenne.
        var mercatorRadius = meters / Math.Cos(lat * Math.PI / 180.0);

        var circle = new NtsPoint(center.X, center.Y).Buffer(mercatorRadius);

        var feature = new GeometryFeature(circle)
        {
            Styles = new List<MStyle>
            {
                new MVectorStyle
                {
                    Fill = new MBrush(new MColor(29, 158, 117, 38)),
                    Outline = new MPen(new MColor(29, 158, 117, 170), 2)
                }
            }
        };

        return new MemoryLayer("Sugár") { Features = new[] { feature } };
    }

    // 6. kritérium: sötét mód. Az OSM alapcsempék csak világosban léteznek,
    // ezért egy félig átlátszó sötét poligont teszünk a csempék fölé.
    static MemoryLayer CreateVeilLayer()
    {
        var ring = new LinearRing(new[]
        {
            new Coordinate(-MercatorExtent, -MercatorExtent),
            new Coordinate(MercatorExtent, -MercatorExtent),
            new Coordinate(MercatorExtent, MercatorExtent),
            new Coordinate(-MercatorExtent, MercatorExtent),
            new Coordinate(-MercatorExtent, -MercatorExtent)
        });

        var feature = new GeometryFeature(new Polygon(ring))
        {
            Styles = new List<MStyle>
            {
                new MVectorStyle
                {
                    Fill = new MBrush(new MColor(12, 16, 24, 170)),
                    Outline = null
                }
            }
        };

        return new MemoryLayer("Sötétítés") { Features = new[] { feature } };
    }

    /// <summary>
    /// A csempeletöltés háttérszálon fut, a rajzolás viszont csak a fő
    /// szálon indítható - ezért a marshalling.
    /// </summary>
    void OnMapDataChanged(object? sender, EventArgs e)
    {
        if (MainThread.IsMainThread)
            MapView.ForceUpdate();
        else
            MainThread.BeginInvokeOnMainThread(MapView.ForceUpdate);
    }

    // 4. kritérium: koppintás a markeren
    void OnMapInfo(object? sender, MapInfoEventArgs e)
    {
        // A csempe- és a sugárréteget szándékosan kihagyjuk a találatkeresésből:
        // különben a kör bárhová koppintva "eltalálódna".
        var feature = e.GetMapInfo(_tappableLayers)?.Feature;

        if (feature?["title"] is null)
        {
            DetailPanel.IsVisible = false;
            return;
        }

        DetailTitle.Text = feature["title"]?.ToString();
        DetailBody.Text = feature["body"]?.ToString();
        DetailPanel.IsVisible = true;
    }

    void OnCloseDetail(object? sender, EventArgs e) => DetailPanel.IsVisible = false;

    void OnDarkModeToggled(object? sender, EventArgs e)
    {
        _darkMode = !_darkMode;

        if (_veilLayer is not null)
            _veilLayer.Enabled = _darkMode;

        DarkModeButton.Text = _darkMode ? "Világos mód" : "Sötét mód";
        AttributionLabel.BackgroundColor = _darkMode ? Color.FromArgb("#B0000000") : Color.FromArgb("#B0FFFFFF");
        AttributionLabel.TextColor = _darkMode ? Colors.White : Color.FromArgb("#222222");

        MapView.Refresh();
    }

    record struct DemoOffer(double DLat, double DLng, string Title, string Body, MColor Fill);

    // 24 marker, determinisztikusan szórva - a valódi felajánlás-sűrűséget utánozza
    static readonly DemoOffer[] DemoOffers = BuildDemoOffers();

    static DemoOffer[] BuildDemoOffers()
    {
        var types = new[]
        {
            ("PET palack", new MColor(29, 158, 117)),
            ("Üvegpalack", new MColor(99, 153, 34)),
            ("Alumínium doboz", new MColor(186, 117, 23)),
            ("Egyéb", new MColor(127, 119, 221))
        };

        var offers = new DemoOffer[24];

        for (var i = 0; i < offers.Length; i++)
        {
            // Aranyszög szerinti spirál: egyenletes szórás véletlenszám nélkül,
            // így a spike futásonként ugyanazt mutatja.
            var angle = i * 2.39996;
            var radius = 0.004 + 0.0035 * i;
            var (name, color) = types[i % types.Length];
            var quantity = 6 + i * 3 % 40;

            offers[i] = new DemoOffer(
                radius * Math.Sin(angle),
                radius * Math.Cos(angle) * 1.5,
                $"{quantity} db {name}",
                $"Felajánlás #{i + 1} · becsült érték {quantity * 50} Ft",
                color);
        }

        return offers;
    }
}
