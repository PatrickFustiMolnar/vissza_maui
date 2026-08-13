using Vissza.Shared.Enums;

namespace Vissza.Api.Entities;

/// <summary>users tábla</summary>
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? Phone { get; set; }

    /// <summary>bcrypt hash. Sosem hagyja el az API-t - lásd UserDto.</summary>
    public string PasswordHash { get; set; } = null!;

    /// <summary>Relatív útvonal az adatbázisban, pl. /uploads/kep.jpg</summary>
    public string? ProfileImage { get; set; }

    public UserRole UserRole { get; set; } = UserRole.Both;
    public string? Bio { get; set; }
    public string? DefaultAddress { get; set; }
    public decimal? DefaultLat { get; set; }
    public decimal? DefaultLng { get; set; }

    public decimal AverageRating { get; set; }
    public int TotalRatings { get; set; }
    public int SuccessfulDonations { get; set; }
    public int SuccessfulCollections { get; set; }

    public bool NotificationsEnabled { get; set; } = true;
    public int NotificationRadius { get; set; } = 5;
    public bool DarkMode { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? LastActivity { get; set; }

    public ICollection<Offer> OffersAsDonor { get; set; } = new List<Offer>();
    public ICollection<CollectionRequest> CollectionRequests { get; set; } = new List<CollectionRequest>();
}
