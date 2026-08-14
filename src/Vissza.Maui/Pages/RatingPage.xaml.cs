using Vissza.Maui.Services;
using Vissza.Maui.ViewModels;

namespace Vissza.Maui.Pages;

[QueryProperty(nameof(TransactionId), "transactionId")]
[QueryProperty(nameof(RatedId), "ratedId")]
[QueryProperty(nameof(RatedName), "ratedName")]
public partial class RatingPage : ContentPage
{
    readonly RatingViewModel _viewModel;

    public RatingPage()
    {
        InitializeComponent();
        BindingContext = _viewModel = ServiceHelper.Get<RatingViewModel>();
    }

    public string TransactionId
    {
        set => _viewModel.TransactionId = int.TryParse(value, out var id) ? id : 0;
    }

    public string RatedId
    {
        set => _viewModel.RatedId = int.TryParse(value, out var id) ? id : 0;
    }

    public string RatedName
    {
        set => _viewModel.RatedName = Uri.UnescapeDataString(value ?? string.Empty);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }
}
