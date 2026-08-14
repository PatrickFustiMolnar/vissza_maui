using Vissza.Shared.Dtos;

namespace Vissza.Maui.Services;

/// <summary>
/// A felhasználó dark_mode beállításának érvényesítése az appon.
///
/// A régi appban ez egy külön ThemeContext volt saját színtáblával; itt elég
/// ennyi, mert a témát maga a MAUI tartja nyilván, és minden szín
/// AppThemeBinding-en keresztül követi.
///
/// Kijelentkezve a rendszertémára állunk vissza: a bejelentkező képernyőnek
/// nincs kihez igazodnia.
/// </summary>
public static class ThemeService
{
    public static void Apply(UserDto? user)
    {
        var theme = user switch
        {
            null => AppTheme.Unspecified,
            { DarkMode: true } => AppTheme.Dark,
            _ => AppTheme.Light
        };

        // A téma az UI állapota, és az AuthStateChanged háttérszálról is
        // jöhet (bejelentkezés, profilmentés), ezért a fő szálra tesszük.
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (Application.Current is { } app)
                app.UserAppTheme = theme;
        });
    }
}
