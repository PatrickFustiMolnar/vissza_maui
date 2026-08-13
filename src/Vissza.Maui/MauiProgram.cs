using Microsoft.Extensions.Logging;
using Refit;
using Vissza.Maui.Pages;
using Vissza.Maui.Services;
using Vissza.Maui.ViewModels;

namespace Vissza.Maui;

public static class MauiProgram
{
    /// <summary>
    /// Az API címe fejlesztéskor. Az Android emulátor saját hálózaton fut,
    /// onnan a gazdagép a 10.0.2.2 címen érhető el - az iOS szimulátor
    /// viszont a gazdagép hálózatát használja.
    ///
    /// Élesben ide az api.fustimolnarpatrick.com kerül majd, HTTPS-en.
    /// </summary>
    static string ApiBaseUrl => DeviceInfo.Platform == DevicePlatform.Android
        ? "http://10.0.2.2:5199"
        : "http://localhost:5199";

    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services.AddSingleton<AuthService>();
        builder.Services.AddTransient<AuthTokenHandler>();

        // A Refit szerializálója pontosan ugyanazokkal a beállításokkal megy,
        // mint a szerveré - lásd ApiJson.
        builder.Services
            .AddRefitClient<IVisszaApi>(new RefitSettings
            {
                ContentSerializer = new SystemTextJsonContentSerializer(ApiJson.Options)
            })
            .ConfigureHttpClient(client =>
            {
                client.BaseAddress = new Uri(ApiBaseUrl);
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .AddHttpMessageHandler<AuthTokenHandler>();


        // Transient, NEM singleton: a Shell egy konkrét ablakhoz kötődik.
        // Singletonként ugyanaz a példány kerülne egy újraépülő jelenet
        // második ablakához is, és a UIKit natívan összeomlik tőle
        // (SIGSEGV a UIWindowScene trait-felépítésében).
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<HomeViewModel>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        var app = builder.Build();

        // A Shell DataTemplate-jei innen érik el a nézetmodelleket.
        ServiceHelper.Initialize(app.Services);

        return app;
    }
}
