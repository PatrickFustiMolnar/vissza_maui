using Mapsui;
using Mapsui.Layers;
using Mapsui.Nts;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Tiling;
using NetTopologySuite.Geometries;
using NtsPoint = NetTopologySuite.Geometries.Point;

// A MainPage.xaml.cs térkép-logikájának ellenőrzése mobil toolchain nélkül.
// Ugyanazokat a Mapsui hívásokat használja, csak MapControl nélkül - így
// kiderül, fordul-e az API és helyes-e a vetítés, Xcode és Android SDK nélkül.

const double Lat = 47.4979;
const double Lng = 19.0402;
const double RadiusMeters = 5000;
const double MercatorExtent = 20037508.34;

var failures = new List<string>();

void Check(string name, bool ok, string detail)
{
    Console.WriteLine($"  {(ok ? "OK  " : "HIBA")}  {name}: {detail}");
    if (!ok) failures.Add(name);
}

static MPoint ToMercator(double lng, double lat)
{
    var (x, y) = SphericalMercator.FromLonLat(lng, lat);
    return new MPoint(x, y);
}

static double HaversineMeters(double lat1, double lng1, double lat2, double lng2)
{
    const double r = 6378137.0;
    var dLat = (lat2 - lat1) * Math.PI / 180.0;
    var dLng = (lng2 - lng1) * Math.PI / 180.0;
    var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
            + Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0)
              * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
    return r * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
}

Console.WriteLine();
Console.WriteLine("1. Vetítés (EPSG:4326 -> EPSG:3857)");

// A várt értéket a Mercator képletéből számoljuk, nem beégetett konstansból,
// így ez valódi független ellenőrzés.
const double EarthRadius = 6378137.0;
var expectedX = EarthRadius * (Lng * Math.PI / 180.0);
var expectedY = EarthRadius * Math.Log(Math.Tan(Math.PI / 4.0 + Lat * Math.PI / 360.0));

var center = ToMercator(Lng, Lat);
Check("Mercator X a képlet szerint", Math.Abs(center.X - expectedX) < 1, $"{center.X:F1} (képlet: {expectedX:F1})");
Check("Mercator Y a képlet szerint", Math.Abs(center.Y - expectedY) < 1, $"{center.Y:F1} (képlet: {expectedY:F1})");

var (backLng, backLat) = SphericalMercator.ToLonLat(center.X, center.Y);
Check("Oda-vissza pontos", Math.Abs(backLat - Lat) < 1e-9 && Math.Abs(backLng - Lng) < 1e-9,
    $"{backLat:F9}, {backLng:F9}");

// A felcserélt paraméter a leggyakoribb Mapsui-hiba: nem hibázik, csak elteszi
// a pontot. Itt bizonyítjuk, hogy tényleg észrevehetetlen lenne futásidőben.
var swapped = ToMercator(Lat, Lng);
var swapError = HaversineMeters(Lat, Lng,
    SphericalMercator.ToLonLat(swapped.X, swapped.Y).lat,
    SphericalMercator.ToLonLat(swapped.X, swapped.Y).lon);
Check("Felcserélt lat/lng csendben hibás", swapError > 1_000_000,
    $"{swapError / 1000:F0} km eltérés, kivétel nélkül");

Console.WriteLine();
Console.WriteLine("2. Sugárkör (NTS Buffer + Mercator korrekció)");

var mercatorRadius = RadiusMeters / Math.Cos(Lat * Math.PI / 180.0);
var circle = new NtsPoint(center.X, center.Y).Buffer(mercatorRadius);

Check("Poligon érvényes", circle.IsValid && circle.Coordinates.Length > 16,
    $"{circle.Coordinates.Length} pont");

// A kör legészakibb és legkeletibb pontját visszavetítjük, és megmérjük a
// valódi távolságot a középponttól. Korrekció nélkül ~1,48-szoros lenne.
var east = circle.Coordinates.OrderByDescending(c => c.X).First();
var (eastLng, eastLat) = SphericalMercator.ToLonLat(east.X, east.Y);
var eastMeters = HaversineMeters(Lat, Lng, eastLat, eastLng);

Check("Valódi sugár keleten", Math.Abs(eastMeters - RadiusMeters) < 250,
    $"{eastMeters:F0} m (cél {RadiusMeters:F0} m)");

var naiveRadius = RadiusMeters;
var naiveEast = new NtsPoint(center.X, center.Y).Buffer(naiveRadius)
    .Coordinates.OrderByDescending(c => c.X).First();
var (naiveLng, naiveLat) = SphericalMercator.ToLonLat(naiveEast.X, naiveEast.Y);
Console.WriteLine($"        korrekció nélkül {HaversineMeters(Lat, Lng, naiveLat, naiveLng):F0} m lenne");

Console.WriteLine();
Console.WriteLine("3. Rétegek és stílusok");

var map = new Map();
map.Layers.Add(OpenStreetMap.CreateTileLayer("MapSpike/0.1 (terkep spike)"));

var ring = new LinearRing(new[]
{
    new Coordinate(-MercatorExtent, -MercatorExtent),
    new Coordinate(MercatorExtent, -MercatorExtent),
    new Coordinate(MercatorExtent, MercatorExtent),
    new Coordinate(-MercatorExtent, MercatorExtent),
    new Coordinate(-MercatorExtent, -MercatorExtent)
});

var veil = new MemoryLayer("Sötétítés")
{
    Enabled = false,
    Features = new[]
    {
        new GeometryFeature(new Polygon(ring))
        {
            Styles = new List<IStyle>
            {
                new VectorStyle { Fill = new Brush(new Color(12, 16, 24, 170)), Outline = null }
            }
        }
    }
};
map.Layers.Add(veil);

map.Layers.Add(new MemoryLayer("Sugár")
{
    Features = new[]
    {
        new GeometryFeature(circle)
        {
            Styles = new List<IStyle>
            {
                new VectorStyle
                {
                    Fill = new Brush(new Color(29, 158, 117, 38)),
                    Outline = new Pen(new Color(29, 158, 117, 170), 2)
                }
            }
        }
    }
});

var markers = new List<IFeature>();
for (var i = 0; i < 24; i++)
{
    var angle = i * 2.39996;
    var radius = 0.004 + 0.0035 * i;
    var feature = new PointFeature(ToMercator(
        Lng + radius * Math.Cos(angle) * 1.5,
        Lat + radius * Math.Sin(angle)));

    feature["title"] = $"Felajánlás #{i + 1}";
    feature.Styles = new List<IStyle>
    {
        new SymbolStyle
        {
            SymbolType = SymbolType.Ellipse,
            SymbolScale = 0.9,
            Fill = new Brush(new Color(29, 158, 117)),
            Outline = new Pen(Color.White, 3)
        }
    };
    markers.Add(feature);
}

var offerLayer = new MemoryLayer("Felajánlások") { Features = markers };
map.Layers.Add(offerLayer);

Check("Rétegsorrend", map.Layers.Count == 4,
    string.Join(" -> ", map.Layers.Select(l => l.Name)));
Check("Marker attribútum olvasható", markers[3]["title"] as string == "Felajánlás #4",
    markers[3]["title"]?.ToString() ?? "null");

var withinRadius = markers.Count(f =>
{
    var p = ((PointFeature)f).Point;
    var (lo, la) = SphericalMercator.ToLonLat(p.X, p.Y);
    return HaversineMeters(Lat, Lng, la, lo) <= RadiusMeters;
});
Check("Markerek a sugáron belül/kívül szórnak", withinRadius > 0 && withinRadius < markers.Count,
    $"{withinRadius} a 24-ből az 5 km-es körön belül");

// A spike egyik valódi találata: méret nélküli viewporton a Navigator némán
// nem csinál semmit. Nincs kivétel, csak a térkép a 0,0 ponton marad.
map.Navigator.CenterOnAndZoomTo(center, 12);
Check("Középre állítás méret nélkül némán elvész",
    Math.Abs(map.Navigator.Viewport.CenterX) < 1,
    $"X={map.Navigator.Viewport.CenterX:F0} a kért {center.X:F0} helyett, kivétel nélkül");

// A helyes megoldás: megvárni a viewport méretét. A MAUI oldalon ezt a
// Map.ViewportInitialized esemény adja; itt kézzel állítjuk be a méretet.
map.Navigator.SetSize(400, 800);
map.Navigator.CenterOnAndZoomTo(center, 12);
Check("Méret után a középre állítás működik",
    Math.Abs(map.Navigator.Viewport.CenterX - center.X) < 1,
    $"X={map.Navigator.Viewport.CenterX:F0}, felbontás={map.Navigator.Viewport.Resolution}");

Console.WriteLine();
if (failures.Count == 0)
{
    Console.WriteLine("MIND OK - a Mapsui logika fordul és a vetítés helyes.");
    return 0;
}

Console.WriteLine($"{failures.Count} HIBA: {string.Join(", ", failures)}");
return 1;
