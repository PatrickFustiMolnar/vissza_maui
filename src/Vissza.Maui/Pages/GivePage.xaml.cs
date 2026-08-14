using Vissza.Maui.Services;
using Vissza.Maui.ViewModels;

namespace Vissza.Maui.Pages;

public partial class GivePage : ContentPage
{
    readonly GiveViewModel _viewModel;

    public GivePage()
    {
        InitializeComponent();
        BindingContext = _viewModel = ServiceHelper.Get<GiveViewModel>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }
}
