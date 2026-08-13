using Microsoft.EntityFrameworkCore;
using Vissza.Api.Entities;
using Vissza.Shared.Enums;

namespace Vissza.Api.Data;

/// <summary>
/// A schema.sql leképezése. A séma nem változik: ez a modell hozzá igazodik,
/// nem fordítva. Migrációt szándékosan nem generálunk - amíg a régi Express
/// backend is ugyanezt az adatbázist használja, egy EF migráció alóla húzná
/// ki a talajt.
///
/// A tábla- és oszlopneveket a UseSnakeCaseNamingConvention() képezi le
/// (Program.cs), így itt csak a kivételek és a kapcsolatok szerepelnek.
/// </summary>
public class VisszaDbContext(DbContextOptions<VisszaDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Offer> Offers => Set<Offer>();
    public DbSet<CollectionRequest> CollectionRequests => Set<CollectionRequest>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Rating> Ratings => Set<Rating>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<ReturnLocation> ReturnLocations => Set<ReturnLocation>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        ConfigureUser(b);
        ConfigureOffer(b);
        ConfigureCollectionRequest(b);
        ConfigureTransaction(b);
        ConfigureRating(b);
        ConfigureMessage(b);
        ConfigureReturnLocation(b);
    }

    static void ConfigureUser(ModelBuilder b) => b.Entity<User>(e =>
    {
        e.Property(x => x.UserRole).HasConversion(new LowerCaseEnumConverter<UserRole>());

        e.Property(x => x.DefaultLat).HasPrecision(10, 8);
        e.Property(x => x.DefaultLng).HasPrecision(11, 8);
        e.Property(x => x.AverageRating).HasPrecision(3, 2);

        e.HasIndex(x => x.Email).IsUnique();
        e.HasIndex(x => new { x.DefaultLat, x.DefaultLng });

        DatabaseGeneratedTimestamps(e);
    });

    static void ConfigureOffer(ModelBuilder b) => b.Entity<Offer>(e =>
    {
        e.Property(x => x.BottleType).HasConversion(new LowerCaseEnumConverter<BottleType>());
        e.Property(x => x.Status).HasConversion(new LowerCaseEnumConverter<OfferStatus>());

        e.Property(x => x.LocationLat).HasPrecision(10, 8);
        e.Property(x => x.LocationLng).HasPrecision(11, 8);

        e.HasOne(x => x.Donor)
            .WithMany(u => u.OffersAsDonor)
            .HasForeignKey(x => x.DonorId)
            .OnDelete(DeleteBehavior.Cascade);

        // A séma itt SET NULL-t használ, ezért nem Cascade: a gyűjtő fiókjának
        // törlése nem viheti magával a felajánlást, ami a felajánlóé.
        e.HasOne(x => x.SelectedCollector)
            .WithMany()
            .HasForeignKey(x => x.SelectedCollectorId)
            .OnDelete(DeleteBehavior.SetNull);

        e.HasIndex(x => x.Status);
        e.HasIndex(x => new { x.LocationLat, x.LocationLng });

        DatabaseGeneratedTimestamps(e);
    });

    static void ConfigureCollectionRequest(ModelBuilder b) => b.Entity<CollectionRequest>(e =>
    {
        e.Property(x => x.Status).HasConversion(new LowerCaseEnumConverter<RequestStatus>());

        e.HasOne(x => x.Offer)
            .WithMany(o => o.CollectionRequests)
            .HasForeignKey(x => x.OfferId)
            .OnDelete(DeleteBehavior.Cascade);

        e.HasOne(x => x.Collector)
            .WithMany(u => u.CollectionRequests)
            .HasForeignKey(x => x.CollectorId)
            .OnDelete(DeleteBehavior.Cascade);

        // Egy gyűjtő ugyanarra a felajánlásra csak egyszer jelentkezhet.
        e.HasIndex(x => new { x.OfferId, x.CollectorId }).IsUnique();

        DatabaseGeneratedTimestamps(e);
    });

    static void ConfigureTransaction(ModelBuilder b) => b.Entity<Transaction>(e =>
    {
        e.Property(x => x.BottleType).HasConversion(new LowerCaseEnumConverter<BottleType>());
        e.Property(x => x.Status).HasConversion(new LowerCaseEnumConverter<TransactionStatus>());

        e.HasOne(x => x.Offer)
            .WithMany()
            .HasForeignKey(x => x.OfferId)
            .OnDelete(DeleteBehavior.Cascade);

        // Mindkét fél NoAction, különben az EF több kaszkádoló utat látna a
        // users táblából ugyanide, amit a MySQL nem enged.
        e.HasOne(x => x.Donor)
            .WithMany()
            .HasForeignKey(x => x.DonorId)
            .OnDelete(DeleteBehavior.NoAction);

        e.HasOne(x => x.Collector)
            .WithMany()
            .HasForeignKey(x => x.CollectorId)
            .OnDelete(DeleteBehavior.NoAction);

        e.HasIndex(x => x.Status);

        DatabaseGeneratedTimestamps(e);
    });

    static void ConfigureRating(ModelBuilder b) => b.Entity<Rating>(e =>
    {
        e.HasOne(x => x.Rater)
            .WithMany()
            .HasForeignKey(x => x.RaterId)
            .OnDelete(DeleteBehavior.NoAction);

        e.HasOne(x => x.Rated)
            .WithMany()
            .HasForeignKey(x => x.RatedId)
            .OnDelete(DeleteBehavior.NoAction);

        e.HasOne(x => x.Transaction)
            .WithMany(t => t.Ratings)
            .HasForeignKey(x => x.TransactionId)
            .OnDelete(DeleteBehavior.SetNull);

        // Egy tranzakció után mindkét fél pontosan egyszer értékelhet.
        e.HasIndex(x => new { x.TransactionId, x.RaterId, x.RatedId }).IsUnique();

        e.Property(x => x.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAdd();
    });

    static void ConfigureMessage(ModelBuilder b) => b.Entity<Message>(e =>
    {
        // A `read` foglalt szó az SQL-ben, a sémában backtickelve szerepel.
        e.Property(x => x.IsRead).HasColumnName("read");

        e.HasOne(x => x.Offer)
            .WithMany()
            .HasForeignKey(x => x.OfferId)
            .OnDelete(DeleteBehavior.Cascade);

        e.HasOne(x => x.Sender)
            .WithMany()
            .HasForeignKey(x => x.SenderId)
            .OnDelete(DeleteBehavior.NoAction);

        e.HasOne(x => x.Receiver)
            .WithMany()
            .HasForeignKey(x => x.ReceiverId)
            .OnDelete(DeleteBehavior.NoAction);

        e.HasIndex(x => x.ReceiverId);
        e.HasIndex(x => x.IsRead);

        e.Property(x => x.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAdd();
    });

    static void ConfigureReturnLocation(ModelBuilder b) => b.Entity<ReturnLocation>(e =>
    {
        e.Property(x => x.Type).HasConversion(new LowerCaseEnumConverter<LocationType>());

        e.Property(x => x.Lat).HasPrecision(10, 8);
        e.Property(x => x.Lng).HasPrecision(11, 8);

        e.HasIndex(x => new { x.Lat, x.Lng });
        e.HasIndex(x => x.Type);

        DatabaseGeneratedTimestamps(e);
    });

    /// <summary>
    /// A created_at és updated_at értékét az adatbázis adja (DEFAULT
    /// CURRENT_TIMESTAMP, illetve ON UPDATE CURRENT_TIMESTAMP). Ha az EF is
    /// írná őket, felülírná a szerver óráját a klienséével.
    /// </summary>
    static void DatabaseGeneratedTimestamps<T>(
        Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<T> e)
        where T : class
    {
        e.Property("CreatedAt")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAdd();

        e.Property("UpdatedAt")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAddOrUpdate();
    }
}
