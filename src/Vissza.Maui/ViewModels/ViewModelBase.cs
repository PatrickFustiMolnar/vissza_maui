using CommunityToolkit.Mvvm.ComponentModel;
using Vissza.Maui.Services;

namespace Vissza.Maui.ViewModels;

/// <summary>
/// A nézetmodellek közös alapja: betöltésjelző és hibaüzenet.
///
/// A <see cref="RunAsync"/> köré fogott műveletek nem tudnak kezeletlen
/// kivétellel elszállni, és a hibaüzenet a szerver saját szövege lesz -
/// nem egy nyers státuszkód.
/// </summary>
public abstract partial class ViewModelBase : ObservableObject
{
    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    partial void OnErrorMessageChanged(string? value) => OnPropertyChanged(nameof(HasError));

    /// <summary>
    /// Egy hálózati művelet futtatása betöltésjelzővel és hibakezeléssel.
    /// Visszatér azzal, hogy sikerült-e.
    /// </summary>
    protected async Task<bool> RunAsync(Func<Task> operation)
    {
        if (IsBusy)
            return false;

        IsBusy = true;
        ErrorMessage = null;

        try
        {
            await operation();
            return true;
        }
        catch (Exception ex)
        {
            ErrorMessage = ApiErrors.Describe(ex);
            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
