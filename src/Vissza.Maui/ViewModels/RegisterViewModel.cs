using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vissza.Maui.Services;
using Vissza.Shared.Dtos;
using Vissza.Shared.Enums;

namespace Vissza.Maui.ViewModels;

public sealed partial class RegisterViewModel(AuthService auth) : ViewModelBase
{
    /// <summary>A szerver is ezt várja el; a régi kliens is így ellenőrizte.</summary>
    const int MinPasswordLength = 6;

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Email { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Password { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ConfirmPassword { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Phone { get; set; } = string.Empty;

    /// <summary>
    /// A szerepkör-választó elemei. Az alapértelmezés a Both, ahogy a régi
    /// appban: a legtöbben mindkét irányban használják.
    /// </summary>
    public IReadOnlyList<string> RoleOptions { get; } =
    [
        "Mindkettő",
        "Csak felajánló",
        "Csak gyűjtő"
    ];

    [ObservableProperty]
    public partial int SelectedRoleIndex { get; set; }

    UserRole SelectedRole => SelectedRoleIndex switch
    {
        1 => UserRole.Donor,
        2 => UserRole.Collector,
        _ => UserRole.Both
    };

    [RelayCommand]
    async Task RegisterAsync()
    {
        // A hibaüzenetek szó szerint a régi appból jönnek, hogy a felhasználó
        // ne találkozzon új megfogalmazással ugyanarra a helyzetre.
        if (string.IsNullOrWhiteSpace(Name)
            || string.IsNullOrWhiteSpace(Email)
            || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Kérlek töltsd ki a kötelező mezőket";
            return;
        }

        if (Password.Length < MinPasswordLength)
        {
            ErrorMessage = $"A jelszónak legalább {MinPasswordLength} karakter hosszúnak kell lennie";
            return;
        }

        if (Password != ConfirmPassword)
        {
            ErrorMessage = "A jelszavak nem egyeznek";
            return;
        }

        var request = new RegisterRequest
        {
            Name = Name.Trim(),
            Email = Email.Trim(),
            Password = Password,
            Phone = string.IsNullOrWhiteSpace(Phone) ? null : Phone.Trim(),
            UserRole = SelectedRole
        };

        if (!await RunAsync(() => auth.RegisterAsync(request)))
            return;

        Password = string.Empty;
        ConfirmPassword = string.Empty;

        await Shell.Current.GoToAsync("//home");
    }

    [RelayCommand]
    async Task GoToSignInAsync()
    {
        try
        {
            await Shell.Current.GoToAsync("//login");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Navigációs hiba: {ex.Message}";
        }
    }
}
