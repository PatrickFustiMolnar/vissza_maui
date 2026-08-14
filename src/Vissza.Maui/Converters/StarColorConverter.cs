using System.Globalization;

namespace Vissza.Maui.Converters;

/// <summary>
/// A kitöltött csillag arany, a többi halvány. A csillag-arany szándékosan
/// témafüggetlen: az értékelés mindkét témában ugyanaz az arany.
/// </summary>
public sealed class StarColorConverter : IValueConverter
{
    static readonly Color Filled = Color.FromArgb("#FBBF24");
    static readonly Color Empty = Color.FromArgb("#9CA3AF");

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? Filled : Empty;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
