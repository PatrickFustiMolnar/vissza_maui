using System.Globalization;

namespace Vissza.Maui.Converters;

/// <summary>Láthatóság kötése egy objektum meglétéhez.</summary>
public sealed class NotNullConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is not null;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
