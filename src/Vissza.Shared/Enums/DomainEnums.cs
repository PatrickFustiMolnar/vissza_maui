namespace Vissza.Shared.Enums;

// A hat enum egy fájlban van, mert mindegyik egyetlen sor, és mind ugyanazt a
// dolgot csinálja: leképezi a schema.sql egy ENUM oszlopát.
//
// Minden tag neve az adatbázisbeli érték kisbetűs alakja - erre épül a
// szerializálás és az EF konverter is. Ha valaha olyan érték kell, ami nem
// pusztán kisbetűs változat, akkor explicit leképezést kell írni mindkét
// helyen (LowerCaseEnumConverter és a JSON policy).

/// <summary>users.user_role</summary>
public enum UserRole
{
    Donor,
    Collector,
    Both
}

/// <summary>offers.bottle_type, transactions.bottle_type</summary>
public enum BottleType
{
    Pet,
    Glass,
    Aluminum,
    Other
}

/// <summary>offers.status</summary>
public enum OfferStatus
{
    Active,
    Reserved,
    Completed,
    Cancelled
}

/// <summary>collection_requests.status</summary>
public enum RequestStatus
{
    Pending,
    Accepted,
    Rejected,
    Cancelled
}

/// <summary>transactions.status</summary>
public enum TransactionStatus
{
    Pending,
    Completed,
    Cancelled
}

/// <summary>return_locations.type</summary>
public enum LocationType
{
    Automata,
    Uzlet,
    Gyujtopont
}
