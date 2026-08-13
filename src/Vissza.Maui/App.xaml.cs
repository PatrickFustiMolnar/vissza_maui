namespace Vissza.Maui;

public partial class App : Application
{
    public App() => InitializeComponent();

    /// <summary>
    /// A szolgáltatásokat az aktivációs állapotból kérjük, nem konstruktor-
    /// befecskendezéssel. Az IServiceProvider App-ba injektálása iOS-en natív
    /// összeomlást okozott a jelenet felépítése közben (SIGSEGV a
    /// UIWindowScene trait-beállításában) - lásd MAUI_TERV.md.
    ///
    /// Navigálni innen nem szabad: a Shell ilyenkor még nincs a jelenethez
    /// kötve. A munkamenet visszaállítása az AppShell-ben fut.
    /// </summary>
    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell());
    }
}
