namespace Vissza.Api.Entities;

/// <summary>
/// ratings tábla.
///
/// A (transaction_id, rater_id, rated_id) hármas egyedi: egy tranzakció után
/// mindkét fél pontosan egyszer értékelheti a másikat.
/// </summary>
public class Rating
{
    public int Id { get; set; }
    public int RaterId { get; set; }
    public int RatedId { get; set; }

    /// <summary>
    /// ON DELETE SET NULL: a tranzakció törlése nem viszi magával az
    /// értékelést, mert az a felhasználó átlagában már benne van.
    /// </summary>
    public int? TransactionId { get; set; }

    /// <summary>1 és 5 között - a séma CHECK megszorítása is őrzi.</summary>
    public int Stars { get; set; }

    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }

    public User Rater { get; set; } = null!;
    public User Rated { get; set; } = null!;
    public Transaction? Transaction { get; set; }
}
