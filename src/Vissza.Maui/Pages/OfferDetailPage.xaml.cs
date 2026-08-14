using Vissza.Maui.Services;
using Vissza.Maui.ViewModels;

namespace Vissza.Maui.Pages;

[QueryProperty(nameof(OfferId), "offerId")]
public partial class OfferDetailPage : ContentPage
{
    readonly OfferDetailViewModel _viewModel;

    public OfferDetailPage()
    {
        InitializeComponent();
        BindingContext = _viewModel = ServiceHelper.Get<OfferDetailViewModel>();
    }

    public string OfferId
    {
        set => _viewModel.OfferId = int.TryParse(value, out var id) ? id : 0;
    }

    /// <summary>
    /// Minden megjelenéskor újratöltünk: a felajánlás állapota a beszélgetés
    /// vagy a felajánló döntése miatt közben megváltozhatott.
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }
}
