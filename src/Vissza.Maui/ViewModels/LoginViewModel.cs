using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vissza.Maui.Services;

namespace Vissza.Maui.ViewModels;

public sealed partial class LoginViewModel(AuthService auth) : ViewModelBase
{
    [ObservableProperty]
    public partial string Email { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Password { get; set; } = string.Empty;

    [RelayCommand]
    async Task SignInAsync()
    {
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Add meg az e-mail címed és a jelszavad.";
            return;
        }

        var success = await RunAsync(() => auth.SignInAsync(Email.Trim(), Password));

        if (!success)
            return;

        // A jelszó nem marad a memóriában a bejelentkezés után.
        Password = string.Empty;

        await Shell.Current.GoToAsync("//home");
    }

    [RelayCommand]
    static Task GoToRegisterAsync() => Shell.Current.GoToAsync("//register");
}
