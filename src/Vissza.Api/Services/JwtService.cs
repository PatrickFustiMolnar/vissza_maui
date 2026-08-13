using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Vissza.Api.Services;

/// <summary>
/// A tokenek kiadása és a titok ellenőrzése.
///
/// A kiadott token alakja szándékosan megegyezik a régi Express backendével
/// (HS256, "id" és "email" claim, 7 nap), így az átállás alatt a két API
/// ugyanazokat a tokeneket fogadja el - feltéve hogy a titok is ugyanaz.
/// </summary>
public sealed class JwtService
{
    public const int MinSecretLength = 32;
    public static readonly TimeSpan TokenLifetime = TimeSpan.FromDays(7);

    static readonly string[] KnownPlaceholders =
    [
        "your-secret-key-change-this-in-production",
        "change-me",
        "secret"
    ];

    readonly SymmetricSecurityKey _key;

    public JwtService(string secret)
    {
        _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
    }

    /// <summary>
    /// Fail-fast a titokra. A korábbi backend legrosszabb hibája az volt, hogy
    /// hiányzó titok esetén egy publikusan ismert kulccsal írt alá, és
    /// hibátlanul működött tovább. Inkább el se induljon.
    /// </summary>
    public static string ReadSecretOrThrow(IConfiguration configuration)
    {
        var secret = configuration["Jwt:Secret"];

        var problem = secret switch
        {
            null or "" => "hiányzik",
            var s when KnownPlaceholders.Contains(s) => "ismert helykitöltő érték",
            var s when s.Length < MinSecretLength =>
                $"túl rövid ({s.Length} karakter, minimum {MinSecretLength})",
            _ => null
        };

        if (problem is not null)
        {
            throw new InvalidOperationException(
                $"A JWT titok {problem}. Állítsd be a Jwt:Secret értéket " +
                "(user secrets vagy Jwt__Secret környezeti változó). " +
                "Generálás: openssl rand -base64 48");
        }

        return secret!;
    }

    public TokenValidationParameters ValidationParameters => new()
    {
        IssuerSigningKey = _key,
        ValidateIssuerSigningKey = true,

        // A régi backend sem állított issuert és audience-t, ezért itt sem
        // ellenőrizzük - különben a meglévő tokenek érvénytelenné válnának.
        ValidateIssuer = false,
        ValidateAudience = false,

        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromSeconds(30)
    };

    public string CreateToken(int userId, string email)
    {
        var descriptor = new SecurityTokenDescriptor
        {
            Claims = new Dictionary<string, object>
            {
                ["id"] = userId,
                ["email"] = email
            },
            Expires = DateTime.UtcNow.Add(TokenLifetime),
            SigningCredentials = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256)
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// A bejelentkezett felhasználó azonosítója a token "id" claimjéből.
    /// A végpontok RequireAuthorization mögött futnak, ezért ha ez hiányzik,
    /// az programozói hiba, nem felhasználói.
    /// </summary>
    public static int GetUserId(this ClaimsPrincipal principal)
    {
        var raw = principal.FindFirstValue("id");

        return int.TryParse(raw, out var id)
            ? id
            : throw new InvalidOperationException("A tokenből hiányzik az \"id\" claim.");
    }
}
