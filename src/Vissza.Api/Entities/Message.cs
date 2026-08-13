namespace Vissza.Api.Entities;

/// <summary>messages tábla</summary>
public class Message
{
    public int Id { get; set; }

    /// <summary>
    /// Nullázható: az üzenet kötődhet egy felajánláshoz, de nem muszáj.
    /// </summary>
    public int? OfferId { get; set; }

    public int SenderId { get; set; }
    public int ReceiverId { get; set; }
    public string Content { get; set; } = null!;

    /// <summary>
    /// Az oszlop neve a sémában `read`, ami az SQL-ben foglalt szó (ezért van
    /// backtickelve). A C# oldalon IsRead, a leképezés a DbContextben van.
    /// </summary>
    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; }

    public Offer? Offer { get; set; }
    public User Sender { get; set; } = null!;
    public User Receiver { get; set; } = null!;
}
