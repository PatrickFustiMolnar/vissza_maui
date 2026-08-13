using Vissza.Shared.Enums;

namespace Vissza.Api.Entities;

/// <summary>
/// collection_requests tábla - egy gyűjtő jelentkezése egy felajánlásra.
///
/// A táblán van egy (offer_id, collector_id) egyedi kulcs: egy gyűjtő
/// ugyanarra a felajánlásra csak egyszer jelentkezhet.
/// </summary>
public class CollectionRequest
{
    public int Id { get; set; }
    public int OfferId { get; set; }
    public int CollectorId { get; set; }

    /// <summary>
    /// A Cancelled a gyűjtő visszavonása. Ez az érték egy ideig hiányzott a
    /// séma ENUM-jából, és a MariaDB STRICT_TRANS_TABLES nélkül nem hibát
    /// adott, hanem üres sztringre csonkolt. Enumként ez nem fordulhat elő.
    /// </summary>
    public RequestStatus Status { get; set; } = RequestStatus.Pending;

    public string? Message { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Offer Offer { get; set; } = null!;
    public User Collector { get; set; } = null!;
}
