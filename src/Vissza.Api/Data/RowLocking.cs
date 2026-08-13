using Microsoft.EntityFrameworkCore;
using Vissza.Api.Entities;

namespace Vissza.Api.Data;

/// <summary>
/// Sorszintű zárolás (<c>SELECT ... FOR UPDATE</c>). Az EF Core-ban nincs rá
/// beépített API, ezért nyers SQL - a táblanév mindig konstans a kódból, az
/// azonosító paraméterként megy.
///
/// ZÁROLÁSI SORREND: előbb a felajánlás, utána a kérés vagy az átvétel.
/// Ez az egész projektben ugyanez, és nem stílus kérdése: több út is írja
/// mindkét táblát, és fordított sorrendben zárva InnoDB-holtpont lenne
/// belőle - a hívó 500-at kapna, nem 409-et.
/// </summary>
public static class RowLocking
{
    public static Task<Offer?> LockOfferAsync(
        this VisszaDbContext db, int id, CancellationToken ct) =>
        db.Offers
            .FromSqlRaw("SELECT * FROM `offers` WHERE `id` = {0} FOR UPDATE", id)
            .FirstOrDefaultAsync(ct);

    public static Task<CollectionRequest?> LockCollectionRequestAsync(
        this VisszaDbContext db, int id, CancellationToken ct) =>
        db.CollectionRequests
            .FromSqlRaw("SELECT * FROM `collection_requests` WHERE `id` = {0} FOR UPDATE", id)
            .FirstOrDefaultAsync(ct);

    public static Task<Transaction?> LockTransactionAsync(
        this VisszaDbContext db, int id, CancellationToken ct) =>
        db.Transactions
            .FromSqlRaw("SELECT * FROM `transactions` WHERE `id` = {0} FOR UPDATE", id)
            .FirstOrDefaultAsync(ct);

    /// <summary>
    /// Zárolás a sor betöltése nélkül. Akkor kell, amikor csak a zárolási
    /// sorrendet tartjuk be, de az adatra nincs szükség.
    /// </summary>
    public static Task LockOfferOnlyAsync(
        this VisszaDbContext db, int id, CancellationToken ct) =>
        db.Database.ExecuteSqlRawAsync(
            "SELECT `id` FROM `offers` WHERE `id` = {0} FOR UPDATE", [id], ct);
}
