using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vissza.Maui.Services;
using Vissza.Shared.Dtos;

namespace Vissza.Maui.ViewModels;

/// <summary>
/// A 2. fázis ellenőrző képernyője: kilistázza az aktív felajánlásokat.
///
/// Nem végleges képernyő - a Dashboard, a Give és a Collect a 3. fázisban
/// készül. Az a dolga, hogy a teljes láncot bizonyítsa: Refit hívás, Bearer
/// token, snake_case DTO leképezés és az OfferCardView megjelenítése.
/// </summary>
public sealed partial class HomeViewModel(IServiceProvider services, AuthService auth) : ViewModelBase
{
    IVisszaApi Api => services.GetRequiredService<IVisszaApi>();

    public ObservableCollection<OfferDto> Offers { get; } = [];

    [ObservableProperty]
    public partial string Greeting { get; set; } = string.Empty;

    [RelayCommand]
    public async Task LoadAsync()
    {
        Greeting = auth.CurrentUser is { } user
            ? $"Szia, {user.Name}!"
            : string.Empty;

        await RunAsync(async () =>
        {
            var offers = await Api.GetOffersAsync(status: "active");

            Offers.Clear();

            foreach (var offer in offers)
                Offers.Add(offer);
        });
    }

    [RelayCommand]
    async Task SignOutAsync()
    {
        await auth.SignOutAsync();
        Offers.Clear();

        await Shell.Current.GoToAsync("//login");
    }

    [RelayCommand]
    static async Task OpenOfferAsync(OfferDto? offer)
    {
        if (offer is null)
            return;

        // A részletek képernyő a 3. fázisban készül.
        await Shell.Current.DisplayAlertAsync(
            offer.Address,
            $"{offer.Quantity} db · {offer.Status}\nFelajánló: {offer.DonorName}",
            "Bezár");
    }
}
