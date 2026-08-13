using System.Reflection;
using Vissza.Shared.Dtos;
using Vissza.Shared.Json;

namespace Vissza.Api.Endpoints;

/// <summary>
/// Enum query paraméterek feldolgozása.
///
/// A query paraméterek kötése nem megy át a JSON konverteren, ezért itt
/// kell kisbetű-tűrően értelmezni őket - a hibaüzenet viszont ugyanaz,
/// mint amit a kérés törzsében lévő rossz érték adna.
/// </summary>
public static class EnumQuery
{
    public static bool TryParse<TEnum>(string text, out TEnum value, out MessageResponse error)
        where TEnum : struct, Enum
    {
        if (Enum.TryParse(text, ignoreCase: true, out value) && Enum.IsDefined(value))
        {
            error = null!;
            return true;
        }

        var wireName = typeof(TEnum).GetCustomAttribute<WireNameAttribute>()?.Name
            ?? typeof(TEnum).Name.ToLowerInvariant();

        var allowed = string.Join(", ", Enum.GetNames<TEnum>().Select(n => n.ToLowerInvariant()));

        error = new MessageResponse($"Invalid {wireName}. Must be one of: {allowed}");
        return false;
    }
}
