namespace Vissza.Maui.Resources;

/// <summary>
/// A Material Design Icons kódpontjai, névvel.
///
/// A betűkészlet ugyanaz, amit a régi app is használt
/// (react-native-vector-icons/MaterialCommunityIcons), tehát az ikonok
/// pontosan megegyeznek - csak a hivatkozás módja más: ott név szerint ment,
/// itt kódponttal, mert a XAML nem tud névből glyphet keresni.
///
/// A kódpontok a kiegészítő magánhasználatú síkon vannak, ezért nyolcjegyű
/// \U escape-pel írjuk őket. Négyjegyűvel a fordító kettévágja a karaktert,
/// és az ikon helyén doboz jelenik meg.
///
/// Csak azok szerepelnek, amiket használunk. A teljes készlet hétezer ikon.
/// </summary>
public static class Icons
{
    /// <summary>map</summary>
    public const string Map = "\U000F034D";

    /// <summary>gift</summary>
    public const string Gift = "\U000F0E44";

    /// <summary>package-variant</summary>
    public const string Package = "\U000F03D6";

    /// <summary>cog</summary>
    public const string Cog = "\U000F0493";

    /// <summary>message-outline</summary>
    public const string Message = "\U000F0365";

    /// <summary>message</summary>
    public const string MessageFilled = "\U000F0361";

    /// <summary>account</summary>
    public const string Account = "\U000F0004";

    /// <summary>magnify</summary>
    public const string Search = "\U000F0349";

    /// <summary>filter-variant</summary>
    public const string Filter = "\U000F0236";

    /// <summary>crosshairs-gps</summary>
    public const string LocateMe = "\U000F01A4";

    /// <summary>close</summary>
    public const string Close = "\U000F0156";

    /// <summary>close-circle</summary>
    public const string CloseCircle = "\U000F0159";

    /// <summary>chevron-right</summary>
    public const string ChevronRight = "\U000F0142";

    /// <summary>chevron-up</summary>
    public const string ChevronUp = "\U000F0143";

    /// <summary>chevron-down</summary>
    public const string ChevronDown = "\U000F0140";

    /// <summary>arrow-left</summary>
    public const string ArrowLeft = "\U000F004D";

    /// <summary>star</summary>
    public const string Star = "\U000F04CE";

    /// <summary>star-outline</summary>
    public const string StarOutline = "\U000F04D2";

    /// <summary>check-circle</summary>
    public const string CheckCircle = "\U000F05E0";

    /// <summary>check-circle-outline</summary>
    public const string CheckCircleOutline = "\U000F05E1";

    /// <summary>clock-outline</summary>
    public const string Clock = "\U000F0150";

    /// <summary>calendar-start</summary>
    public const string CalendarStart = "\U000F166D";

    /// <summary>calendar-end</summary>
    public const string CalendarEnd = "\U000F166C";

    /// <summary>map-marker</summary>
    public const string MapMarker = "\U000F034E";

    /// <summary>information</summary>
    public const string Information = "\U000F02FC";

    /// <summary>camera</summary>
    public const string Camera = "\U000F0100";

    /// <summary>content-save</summary>
    public const string Save = "\U000F0193";

    /// <summary>logout</summary>
    public const string Logout = "\U000F0343";

    /// <summary>leaf</summary>
    public const string Leaf = "\U000F032A";

    /// <summary>cash-multiple</summary>
    public const string Cash = "\U000F0116";

    /// <summary>recycle</summary>
    public const string Recycle = "\U000F044C";

    /// <summary>tag</summary>
    public const string Tag = "\U000F04F9";

    /// <summary>plus</summary>
    public const string Plus = "\U000F0415";

    /// <summary>send</summary>
    public const string Send = "\U000F048A";

    /// <summary>truck</summary>
    public const string Truck = "\U000F053D";

    /// <summary>delete</summary>
    public const string Delete = "\U000F01B4";
}
