using Vissza.Maui.Services;

namespace Vissza.Maui;

public partial class AppShell : Shell
{
    bool _sessionChecked;

    public AppShell()
    {
        InitializeComponent();

        // A chat nem lap, hanem a lapokról nyíló részletképernyő, ezért
        // külön regisztrált útvonal.
        Routing.RegisterRoute("chat", typeof(Pages.ChatPage));
        Routing.RegisterRoute("transaction", typeof(Pages.TransactionDetailPage));
        Routing.RegisterRoute("rating", typeof(Pages.RatingPage));
    }

    /// <summary>
    /// Az AuthService feloldása szándékosan itt történik, nem konstruktor-
    /// befecskendezéssel: azzal az egész HTTP-verem (Refit kliens,
    /// HttpClientFactory, handlerek) az ablak létrehozása közben épülne fel,
    /// ami iOS-en natív összeomlást okozott. Lásd MAUI_TERV.md.
    ///
    /// A szolgáltatásokat a ServiceHelperből vesszük, nem a Handlerből: ebben
    /// a pillanatban a Shell Handlere még nincs kész, a MauiContext null. Ez
    /// csendben ejtette a munkamenet visszaállítását - az app minden
    /// indításkor a bejelentkező képernyőn kezdett.
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_sessionChecked)
            return;

        _sessionChecked = true;

        var auth = ServiceHelper.Get<AuthService>();

        if (await auth.RestoreSessionAsync())
            await GoToAsync("//home");
    }
}
