using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vissza.Maui.Resources;
using Vissza.Maui.Services;
using Vissza.Shared.Dtos;
using Vissza.Shared.Enums;

namespace Vissza.Maui.ViewModels;

/// <summary>
/// Beállítások: profil, értesítések, megjelenés, kapott értékelések.
/// A SettingsScreen.js leképezése.
/// </summary>
public sealed partial class SettingsViewModel(
    IServiceProvider services,
    AuthService auth,
    GeocodingService geocoding) : ViewModelBase
{
    IVisszaApi Api => services.GetRequiredService<IVisszaApi>();

    /// <summary>
    /// Betöltés közben a mezők értéke programból változik, nem a
    /// felhasználótól. E nélkül a sötét mód kapcsolója minden betöltéskor
    /// kiküldene egy mentést.
    /// </summary>
    bool _loading;

    /// <summary>A geokódolást csak akkor futtatjuk, ha a cím tényleg változott.</summary>
    string _addressWhenLoaded = string.Empty;

    // --- profilkártya ---

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Email { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? ProfileImage { get; set; }

    public bool HasProfileImage => !string.IsNullOrWhiteSpace(ProfileImage);

    partial void OnProfileImageChanged(string? value) => OnPropertyChanged(nameof(HasProfileImage));

    /// <summary>A monogram akkor látszik, ha nincs kép - ahogy a régi appban.</summary>
    [ObservableProperty]
    public partial string Initials { get; set; } = "?";

    [ObservableProperty]
    public partial string RatingSummary { get; set; } = string.Empty;

    /// <summary>Értékelés nélkül a 0,0 csillag félrevezető lenne, ezért elrejtjük.</summary>
    [ObservableProperty]
    public partial bool HasRating { get; set; }

    [ObservableProperty]
    public partial string DonationsText { get; set; } = "0 felajánlás";

    [ObservableProperty]
    public partial string CollectionsText { get; set; } = "0 gyűjtés";

    // --- profil adatok ---

    [ObservableProperty]
    public partial string Phone { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Bio { get; set; } = string.Empty;

    public IReadOnlyList<string> RoleOptions { get; } =
        [.. Enum.GetValues<UserRole>().Select(DomainLabels.UserRole)];

    [ObservableProperty]
    public partial int SelectedRoleIndex { get; set; }

    UserRole SelectedRole => Enum.GetValues<UserRole>()[SelectedRoleIndex];

    [ObservableProperty]
    public partial string DefaultAddress { get; set; } = string.Empty;

    // --- értesítések ---

    [ObservableProperty]
    public partial bool NotificationsEnabled { get; set; } = true;

    /// <summary>Kilométerben, szövegként: az Entry számbillentyűzettel megy.</summary>
    [ObservableProperty]
    public partial string NotificationRadius { get; set; } = "5";

    // --- megjelenés ---

    /// <summary>
    /// A sötét mód azonnal mentődik, nem a "Mentés" gombra vár. A régi app is
    /// így viselkedett, és van értelme: a hatása azonnal látszik, tehát a
    /// felhasználó egy külön mentéssel nem tudna mit kezdeni.
    /// </summary>
    [ObservableProperty]
    public partial bool DarkMode { get; set; }

    partial void OnDarkModeChanged(bool value)
    {
        if (!_loading)
            _ = SaveDarkModeAsync(value);
    }

    // --- kapott értékelések ---

    public ObservableCollection<RatingItem> Ratings { get; } = [];

    [ObservableProperty]
    public partial bool ShowRatings { get; set; }

    public bool HasNoRatings => Ratings.Count == 0;

    [RelayCommand]
    void ToggleRatings() => ShowRatings = !ShowRatings;

    // --- betöltés ---

    public async Task LoadAsync()
    {
        await RunAsync(async () =>
        {
            // Nem a tárolt felhasználót vesszük: a statisztika és az átlag a
            // szerveren változik (lezárt átvétel, új értékelés), és a
            // Beállítások pont ezeket mutatja.
            var user = await Api.GetMeAsync();

            auth.UpdateCurrentUser(user);
            Fill(user);

            var ratings = await Api.GetRatingsAsync(ratedId: user.Id);

            Ratings.Clear();

            foreach (var rating in ratings)
                Ratings.Add(new RatingItem(rating));

            OnPropertyChanged(nameof(HasNoRatings));
        });
    }

    void Fill(UserDto user)
    {
        _loading = true;

        try
        {
            Name = user.Name;
            Email = user.Email;
            ProfileImage = user.ProfileImage;
            Initials = MakeInitials(user.Name);

            HasRating = user.AverageRating > 0;
            RatingSummary = $"{user.AverageRating.ToString("0.0", CultureInfo.CurrentCulture)} ({user.TotalRatings} értékelés)";
            DonationsText = $"{user.SuccessfulDonations} felajánlás";
            CollectionsText = $"{user.SuccessfulCollections} gyűjtés";

            Phone = user.Phone ?? string.Empty;
            Bio = user.Bio ?? string.Empty;
            SelectedRoleIndex = Array.IndexOf(Enum.GetValues<UserRole>(), user.UserRole);
            DefaultAddress = user.DefaultAddress ?? string.Empty;
            _addressWhenLoaded = DefaultAddress;

            NotificationsEnabled = user.NotificationsEnabled;
            NotificationRadius = user.NotificationRadius.ToString(CultureInfo.InvariantCulture);

            DarkMode = user.DarkMode;
        }
        finally
        {
            _loading = false;
        }
    }

    /// <summary>"Kovács János" → "KJ". Üres névből "?" lesz, nem üres kör.</summary>
    static string MakeInitials(string name)
    {
        var letters = name
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => char.ToUpperInvariant(part[0]));

        var initials = string.Concat(letters);

        return initials.Length == 0 ? "?" : initials;
    }

    // --- mentés ---

    [RelayCommand]
    async Task SaveAsync()
    {
        if (!int.TryParse(NotificationRadius, out var radius) || radius is < 1 or > 100)
        {
            ErrorMessage = "Az értesítési sugár 1 és 100 km között lehet.";
            return;
        }

        var saved = await RunAsync(async () =>
        {
            var request = new UpdateProfileRequest
            {
                Phone = Phone,
                Bio = Bio,
                UserRole = SelectedRole,
                DefaultAddress = DefaultAddress,
                NotificationsEnabled = NotificationsEnabled,
                NotificationRadius = radius
                // A sötét mód itt szándékosan kimarad: azt a kapcsoló
                // már elmentette.
            };

            // A koordinátát csak megváltozott címnél kérdezzük le: a Nominatim
            // másodpercenként egy kérést enged, és a régi érték úgyis jó.
            if (!string.IsNullOrWhiteSpace(DefaultAddress) && DefaultAddress != _addressWhenLoaded)
            {
                if (await geocoding.ResolveAsync(DefaultAddress) is { } coords)
                {
                    request = request with
                    {
                        DefaultLat = (decimal)coords.Lat,
                        DefaultLng = (decimal)coords.Lng
                    };
                }
            }

            var user = await Api.UpdateMeAsync(request);

            auth.UpdateCurrentUser(user);
            Fill(user);
        });

        if (saved)
            await Shell.Current.DisplayAlertAsync("Kész", "A beállítások elmentve.", "Rendben");
    }

    /// <summary>
    /// Külön út, mert a kapcsoló nem a "Mentés" gombra vár. Hiba esetén
    /// visszabillentjük, hogy a látott állapot ne hazudjon a mentettről.
    /// </summary>
    async Task SaveDarkModeAsync(bool value)
    {
        ThemeService.Apply(auth.CurrentUser is { } current ? current with { DarkMode = value } : null);

        try
        {
            var user = await Api.UpdateMeAsync(new UpdateProfileRequest { DarkMode = value });

            auth.UpdateCurrentUser(user);
        }
        catch (Exception ex)
        {
            ErrorMessage = ApiErrors.Describe(ex);

            _loading = true;

            try
            {
                DarkMode = !value;
            }
            finally
            {
                _loading = false;
            }

            ThemeService.Apply(auth.CurrentUser);
        }
    }

    // --- profilkép ---

    /// <summary>
    /// Kép választása a galériából, feltöltés, majd a profil frissítése.
    /// A kamerát a régi app is felkínálta; itt a MediaPicker mindkettőt tudja,
    /// de a jogosultsági kérdés kevesebb, ha egy forrásból indulunk - a
    /// képernyőn ez így is elég.
    /// </summary>
    [RelayCommand]
    async Task ChangePhotoAsync()
    {
        var action = HasProfileImage
            ? await Shell.Current.DisplayActionSheetAsync("Profilkép", "Mégsem", "Törlés", "Kamera", "Galéria")
            : await Shell.Current.DisplayActionSheetAsync("Profilkép", "Mégsem", null, "Kamera", "Galéria");

        switch (action)
        {
            case "Törlés":
                // A PUT /api/auth/me nem tud null-t: a null azt jelenti,
                // "ne módosítsd". Üres sztringgel töröljük - ezt a szerver
                // is így értelmezi.
                await RunAsync(async () =>
                {
                    var user = await Api.UpdateMeAsync(new UpdateProfileRequest { ProfileImage = string.Empty });

                    auth.UpdateCurrentUser(user);
                    Fill(user);
                });
                break;

            case "Kamera":
                await UploadAsync(() => MediaPicker.Default.CapturePhotoAsync());
                break;

            case "Galéria":
                // Csak a többes választás nem elavult; egyre korlátozzuk, és
                // az elsőt vesszük.
                await UploadAsync(async () =>
                    (await MediaPicker.Default.PickPhotosAsync(new MediaPickerOptions { SelectionLimit = 1 }))
                        .FirstOrDefault());
                break;
        }
    }

    async Task UploadAsync(Func<Task<FileResult?>> pick)
    {
        FileResult? file;

        try
        {
            file = await pick();
        }
        catch (Exception ex)
        {
            // Megtagadott jogosultság vagy nem támogatott eszköz. A választás
            // megszakítása viszont nem kivétel, hanem null.
            ErrorMessage = $"A kép kiválasztása nem sikerült: {ex.Message}";
            return;
        }

        if (file is null)
            return;

        await RunAsync(async () =>
        {
            await using var stream = await file.OpenReadAsync();

            var upload = await Api.UploadAsync(
                new Refit.StreamPart(stream, file.FileName, file.ContentType ?? "image/jpeg"));

            var user = await Api.UpdateMeAsync(new UpdateProfileRequest { ProfileImage = upload.Url });

            auth.UpdateCurrentUser(user);
            Fill(user);
        });
    }

    // --- kijelentkezés ---

    [RelayCommand]
    async Task SignOutAsync()
    {
        var confirmed = await Shell.Current.DisplayAlertAsync(
            "Kijelentkezés",
            "Biztosan kijelentkezel?",
            "Kijelentkezés", "Mégsem");

        if (!confirmed)
            return;

        await auth.SignOutAsync();
        await Shell.Current.GoToAsync("//login");
    }
}
