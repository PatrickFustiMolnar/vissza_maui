using Mapsui;
using Mapsui.Layers;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Tiling;
using Vissza.Maui.Maps;
using MapsuiMap = Mapsui.Map;
using MBrush = Mapsui.Styles.Brush;
using MColor = Mapsui.Styles.Color;
using MPen = Mapsui.Styles.Pen;
using MStyle = Mapsui.Styles.IStyle;

namespace Vissza.Maui.Controls;

public partial class VisszaMapView : ContentView
{
    const string PinLayerName = "Tűk";

    /// <summary>Budapest, amíg nincs helymeghatározás.</summary>
    const double FallbackLat = 47.4979;
    const double FallbackLng = 19.0402;

    /// <summary>Méter per képpont. Nagyjából városi nézet.</summary>
    const double DefaultResolution = 12;

    readonly MemoryLayer _pinLayer = new(PinLayerName);

    bool _centered;

    public VisszaMapView()
    {
        InitializeComponent();

        // A térkép AZONNAL felépül, nem egy async betöltés után. A spike-ban
        // a Map késleltetett beállítása mellett a csempék csak az első
        // felhasználói gesztusra rajzolódtak ki; így a vászon már az első
        // méretezéskor kész térképet kap.
        var map = new MapsuiMap();

        map.Layers.Add(OpenStreetMap.CreateTileLayer("Vissza/1.0 (hu.fustimolnarpatrick.vissza)"));
        map.Layers.Add(_pinLayer);

        map.ViewportInitialized += OnViewportInitialized;

        // A csempék aszinkron érkeznek; enélkül a vászon nem rajzolódik újra.
        map.DataChanged += OnDataChanged;

        Map.Map = map;
        Map.Info += OnInfo;
    }

    /// <summary>Koppintás egy tűre. A hívó a Payload alapján dönti el, mit tegyen.</summary>
    public event EventHandler<MapPin>? PinTapped;

    /// <summary>
    /// A kirakandó tűk. Minden beállításnál teljesen újraépül a réteg -
    /// a listák néhány tucat elemesek, nem éri meg különbséget számolni.
    /// </summary>
    public void SetPins(IEnumerable<MapPin> pins)
    {
        var features = new List<IFeature>();

        foreach (var pin in pins)
        {
            var feature = new PointFeature(ToMercator(pin.Longitude, pin.Latitude));
            feature["pin"] = pin;
            feature.Styles = new List<MStyle> { StyleFor(pin.Kind) };
            features.Add(feature);
        }

        _pinLayer.Features = features;
        _pinLayer.DataHasChanged();

        Redraw();
    }

    /// <summary>A kamera ráállítása egy pontra. Csak az első hívás mozgatja.</summary>
    public void CenterOnce(double latitude, double longitude)
    {
        if (_centered)
            return;

        _centered = true;
        CenterOn(latitude, longitude);
    }

    public void CenterOn(double latitude, double longitude)
    {
        var map = Map.Map;

        if (map.Navigator.Viewport.Width <= 0)
        {
            // Méret nélküli viewporton a Navigator némán nem csinál semmit,
            // ezért ilyenkor a ViewportInitialized eseményre halasztjuk.
            _pendingCenter = (latitude, longitude);
            return;
        }

        map.Navigator.CenterOnAndZoomTo(ToMercator(longitude, latitude), DefaultResolution);
        Redraw();
    }

    (double Lat, double Lng)? _pendingCenter;

    void OnViewportInitialized(object? sender, EventArgs e)
    {
        var (lat, lng) = _pendingCenter ?? (FallbackLat, FallbackLng);
        _pendingCenter = null;

        Map.Map.Navigator.CenterOnAndZoomTo(ToMercator(lng, lat), DefaultResolution);
        Redraw();
    }

    void OnDataChanged(object? sender, EventArgs e) => Redraw();

    void Redraw()
    {
        if (MainThread.IsMainThread)
            Map.Refresh();
        else
            MainThread.BeginInvokeOnMainThread(() => Map.Refresh());
    }

    void OnInfo(object? sender, MapInfoEventArgs e)
    {
        // A találatkeresés a Mapsui 5-ben lusta: meg kell adni, mely
        // rétegeken keressen. A csemperéteget kihagyjuk.
        var feature = e.GetMapInfo([_pinLayer])?.Feature;

        if (feature?["pin"] is MapPin pin)
            PinTapped?.Invoke(this, pin);
    }

    /// <summary>
    /// A Mapsui gömbi Mercatorban (EPSG:3857) dolgozik, az adatbázisban
    /// viszont WGS84 fok van. Figyelem: a FromLonLat először hosszúságot
    /// vár - felcserélve nem hibázik, csak rossz helyre teszi a pontot.
    /// </summary>
    static MPoint ToMercator(double longitude, double latitude)
    {
        var (x, y) = SphericalMercator.FromLonLat(longitude, latitude);
        return new MPoint(x, y);
    }

    static SymbolStyle StyleFor(MapPinKind kind) => new()
    {
        SymbolType = SymbolType.Ellipse,
        SymbolScale = kind == MapPinKind.User ? 1.1 : 0.85,
        Fill = new MBrush(ColorFor(kind)),
        Outline = new MPen(MColor.White, 3)
    };

    // A színek a Colors.xaml sötét változatait követik: a térkép csempéi
    // világosak, azokon ezek olvashatóbbak.
    static MColor ColorFor(MapPinKind kind) => kind switch
    {
        MapPinKind.User => new MColor(55, 138, 221),
        MapPinKind.Offer => new MColor(16, 185, 129),
        _ => new MColor(186, 117, 23)
    };
}
