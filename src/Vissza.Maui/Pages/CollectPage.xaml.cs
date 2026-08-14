using Vissza.Maui.Services;
using Vissza.Maui.ViewModels;

namespace Vissza.Maui.Pages;

public partial class CollectPage : ContentPage
{
    readonly CollectViewModel _viewModel;

    bool _loaded;

    public CollectPage()
    {
        InitializeComponent();

        BindingContext = _viewModel = ServiceHelper.Get<CollectViewModel>();

        _viewModel.PinsChanged += (_, pins) => MapView.SetPins(pins);
        _viewModel.CenterRequested += (_, position) => MapView.CenterOn(position.Lat, position.Lng);

        // A térképen koppintott felajánlás ugyanazt a jelentkezési űrlapot
        // nyitja meg, mint a listaelem.
        MapView.PinTapped += (_, pin) =>
        {
            if (pin.Payload is Shared.Dtos.OfferDto offer)
                _viewModel.SelectCommand.Execute(offer);
        };
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
