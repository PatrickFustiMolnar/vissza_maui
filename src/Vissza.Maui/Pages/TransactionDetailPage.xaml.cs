using Vissza.Maui.Services;
using Vissza.Maui.ViewModels;

namespace Vissza.Maui.Pages;

[QueryProperty(nameof(TransactionId), "transactionId")]
public partial class TransactionDetailPage : ContentPage
{
    readonly TransactionDetailViewModel _viewModel;

    public TransactionDetailPage()
    {
        InitializeComponent();
        BindingContext = _viewModel = ServiceHelper.Get<TransactionDetailViewModel>();
    }

    public string TransactionId
    {
        set => _viewModel.TransactionId = int.TryParse(value, out var id) ? id : 0;
    }

    // Minden megjelenéskor újratölt: a chatből visszatérve a másik fél
    // közben megerősíthette az átvételt.
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }
}
