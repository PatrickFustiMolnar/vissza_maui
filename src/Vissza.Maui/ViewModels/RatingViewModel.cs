using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vissza.Maui.Services;
using Vissza.Shared.Dtos;

namespace Vissza.Maui.ViewModels;

/// <summary>
/// Értékelés egy lezárt átvétel után. A RatingScreen.js leképezése.
///
/// Egy tranzakcióra fél-párosonként egy értékelés jut - a szerver ezt egyedi
/// kulccsal őrzi. Ha már van, ez a képernyő azt tölti be szerkesztésre,
/// nem másodikat hoz létre.
/// </summary>
public sealed partial class RatingViewModel(IServiceProvider services, AuthService auth) : ViewModelBase
{
    IVisszaApi Api => services.GetRequiredService<IVisszaApi>();

    RatingDto? _existing;

    [ObservableProperty]
    public partial int TransactionId { get; set; }

    [ObservableProperty]
    public partial int RatedId { get; set; }

    [ObservableProperty]
    public partial string RatedName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int Stars { get; set; }

    [ObservableProperty]
    public partial string Comment { get; set; } = string.Empty;

    public bool IsEditing => _existing is not null;

    public string SubmitText => IsEditing ? "Értékelés módosítása" : "Értékelés küldése";

    // A csillagsor öt külön jelzője. Kötésből egyszerűbb így, mint egy
    // listával, és a XAML is olvashatóbb marad.
    public bool Star1 => Stars >= 1;
    public bool Star2 => Stars >= 2;
    public bool Star3 => Stars >= 3;
    public bool Star4 => Stars >= 4;
    public bool Star5 => Stars >= 5;

    partial void OnStarsChanged(int value)
    {
        foreach (var name in new[] { nameof(Star1), nameof(Star2), nameof(Star3), nameof(Star4), nameof(Star5) })
            OnPropertyChanged(name);
    }

    public async Task LoadAsync()
    {
        if (auth.CurrentUser is not { } user)
            return;

        await RunAsync(async () =>
        {
            var ratings = await Api.GetRatingsAsync(transactionId: TransactionId, raterId: user.Id);

            _existing = ratings.FirstOrDefault(r => r.RatedId == RatedId);

            if (_existing is { } rating)
            {
                Stars = rating.Stars;
                Comment = rating.Comment ?? string.Empty;
            }

            OnPropertyChanged(nameof(IsEditing));
            OnPropertyChanged(nameof(SubmitText));
        });
    }

    [RelayCommand]
    void SetStars(string? value)
    {
        if (int.TryParse(value, out var stars) && stars is >= 1 and <= 5)
            Stars = stars;
    }

    [RelayCommand]
    async Task SubmitAsync()
    {
        if (Stars is < 1 or > 5)
        {
            ErrorMessage = "Válassz 1 és 5 közötti értékelést.";
            return;
        }

        var comment = string.IsNullOrWhiteSpace(Comment) ? null : Comment.Trim();

        var saved = _existing is { } existing
            ? await RunAsync(() => Api.UpdateRatingAsync(existing.Id,
                new UpdateRatingRequest { Stars = Stars, Comment = comment }))
            : await RunAsync(() => Api.CreateRatingAsync(new CreateRatingRequest
            {
                RatedId = RatedId,
                TransactionId = TransactionId,
                Stars = Stars,
                Comment = comment
            }));

        if (!saved)
            return;

        await Shell.Current.DisplayAlertAsync("Köszönjük", "Az értékelésed elmentve.", "Rendben");
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    async Task DeleteAsync()
    {
        if (_existing is not { } existing)
            return;

        var confirmed = await Shell.Current.DisplayAlertAsync(
            "Értékelés törlése",
            "Az értékelésed törlődik, és a partner átlaga újraszámolódik.",
            "Törlés", "Mégsem");

        if (!confirmed)
            return;

        if (await RunAsync(() => Api.DeleteRatingAsync(existing.Id)))
            await Shell.Current.GoToAsync("..");
    }
}
