using Vissza.Maui.Services;
using Vissza.Shared.Dtos;

namespace Vissza.Maui.ViewModels;

/// <summary>
/// Egy kapott értékelés a Beállítások listájában. Csak megjelenítésre való:
/// a csillagokat öt logikai jelzőre bontja, mert a XAML-ből egy szám nem
/// köthető közvetlenül csillagsorra.
/// </summary>
public sealed record RatingItem(RatingDto Rating)
{
    public int Id => Rating.Id;

    /// <summary>A régi API is "Ismeretlen"-t írt, ha az értékelő törölve lett.</summary>
    public string RaterName => Rating.RaterName ?? "Ismeretlen";

    public string? Comment => Rating.Comment;
    public bool HasComment => !string.IsNullOrWhiteSpace(Rating.Comment);

    public string CreatedAtText => Times.ToLocal(Rating.CreatedAt).ToString("yyyy. MM. dd.");

    public bool Star1 => Rating.Stars >= 1;
    public bool Star2 => Rating.Stars >= 2;
    public bool Star3 => Rating.Stars >= 3;
    public bool Star4 => Rating.Stars >= 4;
    public bool Star5 => Rating.Stars >= 5;
}
