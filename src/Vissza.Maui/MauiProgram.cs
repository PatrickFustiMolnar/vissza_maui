using Microsoft.Extensions.Logging;
using Refit;
using SkiaSharp.Views.Maui.Controls.Hosting;
using Vissza.Maui.Pages;
using Vissza.Maui.Services;
using Vissza.Maui.ViewModels;

namespace Vissza.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            // A Mapsui SkiaSharp vászonra rajzol; e nélkül a térkép üres.
            .UseSkiaSharp()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");

                // Ugyanaz a készlet, amit a régi app használt
                // (react-native-vector-icons), tehát az ikonok megegyeznek.
                fonts.AddFont("MaterialCommunityIcons.ttf", "Icons");
            });

        builder.Services.AddSingleton<AuthService>();
        builder.Services.AddTransient<AuthTokenHandler>();

        // A Refit szerializálója pontosan ugyanazokkal a beállításokkal megy,
        // mint a szerveré - lásd ApiJson.
        builder.Services
            .AddRefitGeneratedClient<IVisszaApi>(new RefitSettings
            {
                ContentSerializer = new SystemTextJsonContentSerializer(ApiJson.Options)
            })
            .ConfigureHttpClient(client =>
            {
                client.BaseAddress = new Uri(ApiConfig.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .AddHttpMessageHandler<AuthTokenHandler>();


        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<RegisterViewModel>();
        builder.Services.AddTransient<DashboardViewModel>();
        builder.Services.AddTransient<GiveViewModel>();
        builder.Services.AddTransient<CollectViewModel>();
        builder.Services.AddTransient<ConversationsViewModel>();
        builder.Services.AddTransient<ChatViewModel>();
        builder.Services.AddTransient<TransactionDetailViewModel>();
        builder.Services.AddTransient<RatingViewModel>();
        builder.Services.AddTransient<SettingsViewModel>();
        builder.Services.AddTransient<OfferDetailViewModel>();
        builder.Services.AddSingleton<GeocodingService>();
        builder.Services.AddSingleton<PhotoService>();

        // Egy kapcsolat az egész alkalmazásra: a beszélgetés és a lista is
        // ugyanarra az eseményre iratkozik fel.
        builder.Services.AddSingleton<ChatHubService>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        var app = builder.Build();

        // A Shell DataTemplate-jei innen érik el a nézetmodelleket.
        ServiceHelper.Initialize(app.Services);

        return app;
    }
}
