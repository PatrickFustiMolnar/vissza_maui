using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Vissza.Api.Data;

/// <summary>
/// Enum és MySQL ENUM oszlop közötti átváltás.
///
/// Az EF beépített <c>HasConversion&lt;string&gt;()</c> hívása a tag nevét
/// írná ki ("Donor"), az adatbázisban viszont kisbetűs érték van ("donor").
/// Egy nem egyező érték nem hibát adna: a MariaDB STRICT_TRANS_TABLES
/// nélkül üres sztringre csonkol, és a sor csendben kiesik minden
/// státusz-szűrős lekérdezésből.
///
/// Az olvasás szándékosan kis-nagybetű független, hogy a régi Express
/// backend által beírt sorok is beolvashatók legyenek.
/// </summary>
public sealed class LowerCaseEnumConverter<TEnum> : ValueConverter<TEnum, string>
    where TEnum : struct, Enum
{
    public LowerCaseEnumConverter()
        : base(
            value => value.ToString().ToLowerInvariant(),
            text => Enum.Parse<TEnum>(text, ignoreCase: true))
    {
    }
}
