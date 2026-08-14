using Vissza.Maui.Services;
using Vissza.Maui.ViewModels;

namespace Vissza.Maui.Pages;

/// <summary>
/// A beszélgetés útvonal-paraméterekkel nyílik, hogy a Shell navigáció
/// mélyhivatkozásként is működjön (pl. a felajánlás részleteiből).
/// </summary>
[QueryProperty(nameof(PartnerId), "partnerId")]
[QueryProperty(nameof(PartnerName), "partnerName")]
public partial class ChatPage : ContentPage
{
    readonly ChatViewModel _viewModel;

    public ChatPage()
    {
        InitializeComponent();
        BindingContext = _viewModel = ServiceHelper.Get<ChatViewModel>();
    }

    public string PartnerId
    {
        set => _viewModel.PartnerId = int.TryParse(value, out var id) ? id : 0;
    }

    public string PartnerName
    {
        set => _viewModel.PartnerName = Uri.UnescapeDataString(value ?? string.Empty);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await _viewModel.LoadAsync();
        await _viewModel.ListenAsync();
    }

    // A képernyő elhagyásakor leiratkozunk. Az élő kapcsolat maga megmarad -
    // a beszélgetéslistának is kell -, csak ez a nézet nem hallgatja tovább.
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.StopListening();
    }
}
