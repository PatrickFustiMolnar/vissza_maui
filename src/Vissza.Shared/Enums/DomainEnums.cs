using Vissza.Shared.Json;

namespace Vissza.Shared.Enums;

// A hat enum egy fájlban van, mert mindegyik egyetlen sor, és mind ugyanazt a
// dolgot csinálja: leképezi a schema.sql egy ENUM oszlopát.
//
// Minden tag neve az adatbázisbeli érték kisbetűs alakja - erre épül a
// szerializálás és az EF konverter is. Ha valaha olyan érték kell, ami nem
// pusztán kisbetűs változat, akkor explicit leképezést kell írni mindkét
// helyen (LowerCaseEnumConverter és a JSON policy).

/// <summary>users.user_role</summary>
[WireName("user_role")]
public enum UserRole
{
    Donor,
    Collector,
    Both
}

/// <summary>offers.bottle_type, transactions.bottle_type</summary>
[WireName("bottle_type")]
public enum BottleType
{
    Pet,
    Glass,
    Aluminum,
    Other
}

/// <summary>offers.status</summary>
[WireName("status")]
public enum OfferStatus
{
    Active,
    Reserved,
    Completed,
    Cancelled
}

/// <summary>collection_requests.status</summary>
[WireName("status")]
public enum RequestStatus
{
    Pending,
    Accepted,
    Rejected,
    Cancelled
}

/// <summary>transactions.status</summary>
[WireName("status")]
public enum TransactionStatus
{
    Pending,
    Completed,
    Cancelled
}

/// <summary>return_locations.type</summary>
[WireName("type")]
public enum LocationType
{
    Automata,
    Uzlet,
    Gyujtopont
}
