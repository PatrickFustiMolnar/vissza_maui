using System.Net.Http.Headers;

namespace Vissza.Maui.Services;

/// <summary>
/// Ráteszi a Bearer tokent minden kimenő kérésre.
///
/// Azért handler és nem paraméter minden Refit metóduson: így egyetlen helyen
/// van, és egy új végpont nem tud véletlenül token nélkül maradni.
///
/// Az AuthService-t lusta feloldással kéri, mert az AuthService maga is az
/// IVisszaApi-ra épül - közvetlen függéssel körkörös lenne.
/// </summary>
public sealed class AuthTokenHandler(IServiceProvider services) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // A bejelentkezés és a regisztráció nyilvános, és ilyenkor még nincs
        // token - a null egyszerűen kihagyja a fejlécet.
        var token = services.GetRequiredService<AuthService>().Token;

        if (!string.IsNullOrEmpty(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return base.SendAsync(request, cancellationToken);
    }
}
