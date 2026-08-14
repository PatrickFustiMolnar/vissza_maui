using Vissza.Maui.Services;
using Vissza.Maui.ViewModels;

namespace Vissza.Maui.Pages;

public partial class SettingsPage : ContentPage
{
    readonly SettingsViewModel _viewModel;

    public SettingsPage()
    {
        InitializeComponent();
        BindingContext = _viewModel = ServiceHelper.Get<SettingsViewModel>();
    }

    /// <summary>
    /// Minden megjelenéskor újratöltünk: a statisztika és az átlag a többi
    /// képernyőn végzett munkától változik.
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }
}
