using Vissza.Maui.Services;

namespace Vissza.Maui;

public partial class AppShell : Shell
{
    bool _sessionChecked;

    public AppShell() => InitializeComponent();

    /// <summary>
    /// Az AuthService feloldása szándékosan itt történik, nem konstruktor-
    /// befecskendezéssel: azzal az egész HTTP-verem (Refit kliens,
    /// HttpClientFactory, handlerek) az ablak létrehozása közben épülne fel,
    /// ami iOS-en natív összeomlást okozott. Lásd MAUI_TERV.md.
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_sessionChecked)
            return;

        _sessionChecked = true;

        var services = Handler?.MauiContext?.Services;

        if (services?.GetService<AuthService>() is not { } auth)
            return;

        if (await auth.RestoreSessionAsync())
            await GoToAsync("//home");
    }
}
