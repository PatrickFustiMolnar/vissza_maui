using Vissza.Maui.Services;
using Vissza.Maui.ViewModels;

namespace Vissza.Maui.Pages;

public partial class ConversationsPage : ContentPage
{
    readonly ConversationsViewModel _viewModel;

    public ConversationsPage()
    {
        InitializeComponent();
        BindingContext = _viewModel = ServiceHelper.Get<ConversationsViewModel>();
    }

    // Minden megjelenéskor frissít: a chatből visszatérve az olvasatlan
    // számnak és az utolsó üzenetnek naprakésznek kell lennie.
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await _viewModel.LoadAsync();
        await _viewModel.ListenAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.StopListening();
    }
}
