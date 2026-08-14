using Vissza.Maui.Maps;
using Vissza.Maui.Services;
using Vissza.Maui.ViewModels;

namespace Vissza.Maui.Pages;

public partial class DashboardPage : ContentPage
{
    readonly DashboardViewModel _viewModel;

    bool _loaded;

    public DashboardPage()
    {
        InitializeComponent();

        BindingContext = _viewModel = ServiceHelper.Get<DashboardViewModel>();

        // A térkép nem tud kötésből tűket fogadni, ezért a nézetmodell
        // eseményeken keresztül szól neki. Így a nézetmodell nem ismeri a
        // Mapsuit, és tesztelhető marad nélküle.
        _viewModel.PinsChanged += (_, pins) => MapView.SetPins(pins);
        _viewModel.CenterRequested += (_, position) => MapView.CenterOn(position.Lat, position.Lng);

        MapView.PinTapped += (_, pin) => _viewModel.Select(pin);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_loaded)
            return;

        _loaded = true;
        await _viewModel.LoadAsync();
    }
}
