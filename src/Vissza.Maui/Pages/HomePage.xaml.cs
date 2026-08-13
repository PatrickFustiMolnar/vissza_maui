using Vissza.Maui.Services;
using Vissza.Maui.ViewModels;

namespace Vissza.Maui.Pages;

public partial class HomePage : ContentPage
{
    readonly HomeViewModel _viewModel;

    public HomePage()
    {
        InitializeComponent();
        BindingContext = _viewModel = ServiceHelper.Get<HomeViewModel>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }
}
