using System.Globalization;

namespace Vissza.Maui.Converters;

/// <summary>
/// Logikai tagadás kötéshez: <c>IsEnabled="{Binding IsBusy, Converter={StaticResource Not}}"</c>.
/// Betöltés közben így tiltjuk le a beviteli mezőket és a gombokat.
/// </summary>
public sealed class InvertedBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is not true;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is not true;
}
