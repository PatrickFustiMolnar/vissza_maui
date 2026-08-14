using Vissza.Shared.Dtos;

namespace Vissza.Maui.Services;

/// <summary>
/// A bejelentkezett felhasználó és a token kezelése.
///
/// A token <see cref="SecureStorage"/>-ba kerül, nem sima beállításokba: iOS-en
/// a kulcstartóba, Androidon a Keystore-ral titkosítva. A régi app AsyncStorage-t
/// használt, ami sima szövegként tárolta.
/// </summary>
public sealed class AuthService(IServiceProvider services)
{
    // Az API kliens lusta: a Refit/HttpClient verem felépítése az UI
    // létrehozásának pillanatában natív összeomlást okoz iOS-en, ezért
    // csak az első tényleges hívásnál épül fel.
    IVisszaApi Api => services.GetRequiredService<IVisszaApi>();

    const string TokenKey = "vissza.auth.token";

    string? _token;

    public UserDto? CurrentUser { get; private set; }

    /// <summary>
    /// A bejelentkezett felhasználó cseréje. Minden út ezen megy át, mert a
    /// témát is itt kell érvényesíteni: a Shell életciklusára kötve nem
    /// megbízható, a bejelentkezéskori beállítás kimaradt.
    /// </summary>
    void SetUser(UserDto? user)
    {
        CurrentUser = user;

        ThemeService.Apply(user);
        AuthStateChanged?.Invoke(this, EventArgs.Empty);
    }

    public bool IsSignedIn => CurrentUser is not null;

    /// <summary>Az AuthTokenHandler kérdezi le minden kimenő kéréshez.</summary>
    public string? Token => _token;

    public event EventHandler? AuthStateChanged;

    /// <summary>
    /// Indításkor fut. Ha van eltárolt token, ellenőrzi is: egy lejárt vagy
    /// visszavont tokennel a felhasználó bejelentkezettnek látszana, aztán
    /// minden képernyő 401-re futna.
    /// </summary>
    public async Task<bool> RestoreSessionAsync()
    {
        try
        {
            _token = await SecureStorage.Default.GetAsync(TokenKey);

            if (string.IsNullOrEmpty(_token))
                return false;

            SetUser(await Api.GetMeAsync());

            return true;
        }
        catch (Exception ex)
        {
            // Bármi hiba esetén kijelentkezett állapotból indulunk - ez mindig
            // helyreállítható, szemben egy fél-bejelentkezett állapottal.
            System.Diagnostics.Debug.WriteLine($"A munkamenet visszaállítása nem sikerült: {ex.Message}");
            await SignOutAsync();

            return false;
        }
    }

    public async Task SignInAsync(string email, string password)
    {
        var response = await Api.LoginAsync(new LoginRequest { Email = email, Password = password });

        await StoreAsync(response);
    }

    public async Task RegisterAsync(RegisterRequest request)
    {
        var response = await Api.RegisterAsync(request);

        await StoreAsync(response);
    }

    public async Task SignOutAsync()
    {
        _token = null;

        SecureStorage.Default.Remove(TokenKey);

        // Az élő chat-kapcsolat a régi tokennel épült; a következő
        // felhasználó üzeneteit nem kaphatja meg. A feloldás itt, hívás
        // közben történik - a ChatHubService maga is ezt a szolgáltatást
        // használja, tehát konstruktorban körkörös lenne.
        await services.GetRequiredService<ChatHubService>().StopAsync();

        SetUser(null);
    }

    /// <summary>A profil frissítése után az eltárolt felhasználót is frissítjük.</summary>
    public void UpdateCurrentUser(UserDto user) => SetUser(user);

    async Task StoreAsync(AuthResponse response)
    {
        _token = response.Token;

        await SecureStorage.Default.SetAsync(TokenKey, response.Token);
        SetUser(response.User);
    }
}
