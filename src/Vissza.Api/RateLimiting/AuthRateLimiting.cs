using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Vissza.Shared.Dtos;

namespace Vissza.Api.RateLimiting;

/// <summary>
/// A jelszó ellenőrzése az egyetlen végpont, ahol a próbálkozás önmagában
/// információt ér - limit nélkül a jelszavak szabadon próbálgathatók.
///
/// Két réteg, mert a felhasználók java mobilhálózatról jön, ahol sokan
/// osztoznak egy publikus IP-n:
///   - fiókonként szűk limit fogja meg a célzott jelszóprobálgatást, és nem
///     zár ki mást, aki ugyanarról az IP-ről jelentkezik be;
///   - IP-nként tág limit fogja meg a sok fiókot próbálgató szórást, de a
///     normál használat közelébe sem ér.
/// </summary>
public static class AuthRateLimiting
{
    const string EmailItemKey = "vissza.login-email";

    static readonly TimeSpan Window = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Tágabb, mint a régi Express backend 10-es limitje. Ott a sikeres
    /// bejelentkezés nem számított bele (skipSuccessfulRequests), az
    /// ASP.NET beépített limitere viszont a kérés elején kér engedélyt,
    /// tehát a sikeres próbálkozásokat is számolja.
    /// </summary>
    const int PerAccountLimit = 15;

    const int PerIpLimit = 60;

    /// <summary>A kérés törzséből legfeljebb ennyit olvasunk ki e-mailért.</summary>
    const int MaxBodyPeek = 4 * 1024;

    public static void Configure(RateLimiterOptions options)
    {
        options.GlobalLimiter = PartitionedRateLimiter.CreateChained(
            PartitionedRateLimiter.Create<HttpContext, string>(PerIpPartition),
            PartitionedRateLimiter.Create<HttpContext, string>(PerAccountPartition));

        options.OnRejected = async (context, cancellationToken) =>
        {
            context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

            await context.HttpContext.Response.WriteAsJsonAsync(
                new MessageResponse("Too many attempts. Please try again later."),
                cancellationToken);
        };
    }

    static RateLimitPartition<string> PerIpPartition(HttpContext context)
    {
        if (!IsAuthAttempt(context))
            return RateLimitPartition.GetNoLimiter("skip");

        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return FixedWindow($"ip:{ip}", PerIpLimit);
    }

    static RateLimitPartition<string> PerAccountPartition(HttpContext context)
    {
        if (!IsAuthAttempt(context))
            return RateLimitPartition.GetNoLimiter("skip");

        // Az e-mailt a UseLoginAttemptIdentity middleware olvasta ki - itt a
        // partícióválasztó szinkron, a törzs olvasása pedig nem az.
        var email = context.Items[EmailItemKey] as string ?? "";
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return FixedWindow($"account:{ip}:{email}", PerAccountLimit);
    }

    static RateLimitPartition<string> FixedWindow(string key, int limit) =>
        RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = limit,
            Window = Window,
            QueueLimit = 0
        });

    static bool IsAuthAttempt(HttpContext context) =>
        HttpMethods.IsPost(context.Request.Method)
        && (context.Request.Path.StartsWithSegments("/api/auth/login")
            || context.Request.Path.StartsWithSegments("/api/auth/register"));

    /// <summary>
    /// Kiolvassa a bejelentkezési kísérlet e-mail címét, hogy a fiókonkénti
    /// limiter elérhesse. A törzset visszatekeri, így a végpont modellkötése
    /// változatlanul működik.
    /// </summary>
    public static IApplicationBuilder UseLoginAttemptIdentity(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            if (IsAuthAttempt(context))
                context.Items[EmailItemKey] = await PeekEmailAsync(context.Request);

            await next();
        });

    static async Task<string> PeekEmailAsync(HttpRequest request)
    {
        if (!request.HasJsonContentType())
            return "";

        try
        {
            request.EnableBuffering();

            var buffer = new byte[MaxBodyPeek];
            var read = await request.Body.ReadAtLeastAsync(buffer, MaxBodyPeek, throwOnEndOfStream: false);
            request.Body.Position = 0;

            using var document = JsonDocument.Parse(buffer.AsMemory(0, read));

            return document.RootElement.TryGetProperty("email", out var email)
                && email.ValueKind == JsonValueKind.String
                    ? email.GetString()!.Trim().ToLowerInvariant()
                    : "";
        }
        catch (JsonException)
        {
            // Hibás JSON: a végpont úgyis 400-at ad. A limitelés szempontjából
            // ez egy azonosítatlan kísérlet, ami az üres kulcsra esik.
            return "";
        }
    }
}
