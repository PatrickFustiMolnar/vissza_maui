using System.Text.Json;
using Vissza.Shared.Dtos;

namespace Vissza.Api.Middleware;

/// <summary>
/// Egységes hibaalak: minden hiba <c>{ "message": "..." }</c> formában megy
/// vissza, ahogy a régi Express backendnél.
///
/// E nélkül a modellkötés hibái (hibás JSON, tartományon kívüli enum érték)
/// RFC 7807 problem details válaszként mennének, amit a kliens nem ismer.
/// </summary>
public static class ErrorHandling
{
    public static IApplicationBuilder UseMessageShapedErrors(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            try
            {
                await next();
            }
            catch (BadHttpRequestException ex)
            {
                await WriteAsync(context, StatusCodes.Status400BadRequest, DescribeBadRequest(ex));
            }
            catch (Exception ex)
            {
                var logger = context.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger(nameof(ErrorHandling));

                logger.LogError(ex, "Kezeletlen hiba: {Method} {Path}",
                    context.Request.Method, context.Request.Path);

                // A kivétel szövege csak fejlesztői módban megy vissza. A régi
                // backend élesben is kiadta, ami veremnyomokat és SQL
                // részleteket szivárogtatott a kliensnek.
                var environment = context.RequestServices.GetRequiredService<IHostEnvironment>();

                await WriteAsync(context, StatusCodes.Status500InternalServerError,
                    environment.IsDevelopment() ? $"Server error: {ex.Message}" : "Server error");
            }
        });

    /// <summary>
    /// A modellkötés hibáiból csak azt engedjük ki, ami a kérésről szól.
    ///
    /// A saját DomainEnumConverterünk beszédes üzenetet dob ("Invalid
    /// bottle_type. Must be one of: ..."), amihez a System.Text.Json hozzáfűzi
    /// a " Path: $.mezo | LineNumber: ..." részt - azt levágjuk. Minden más
    /// JSON-hiba szövege .NET típusneveket szivárogtatna, ezért általános
    /// üzenetre cseréljük.
    /// </summary>
    static string DescribeBadRequest(BadHttpRequestException exception)
    {
        const string ownMessagePrefix = "Invalid ";

        if (exception.InnerException is not JsonException json || json.Message is null)
            return "Invalid request body";

        if (!json.Message.StartsWith(ownMessagePrefix, StringComparison.Ordinal))
            return "Invalid request body";

        var pathMarker = json.Message.IndexOf(" Path: ", StringComparison.Ordinal);

        return pathMarker < 0 ? json.Message : json.Message[..pathMarker];
    }

    static async Task WriteAsync(HttpContext context, int statusCode, string message)
    {
        // Ha a válasz már elindult, nem lehet státuszt írni - ilyenkor csak a
        // napló marad, a kapcsolat pedig megszakad.
        if (context.Response.HasStarted)
            return;

        context.Response.Clear();
        context.Response.StatusCode = statusCode;

        await context.Response.WriteAsJsonAsync(new MessageResponse(message));
    }
}
