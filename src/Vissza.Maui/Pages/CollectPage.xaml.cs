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

        // A térképen koppintott tű a részletlapot nyitja, ahogy a régi appban
        // is: a tűn nincs mit olvasni, tehát ott a teljes lap a hasznos. A
        // listaelem viszont már mutatja a lényeget, ezért az továbbra is a
        // rövid jelentkezési űrlapot nyitja.
        MapView.PinTapped += (_, pin) =>
        {
            if (pin.Payload is Shared.Dtos.OfferDto offer)
                _viewModel.OpenDetailCommand.Execute(offer);
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
