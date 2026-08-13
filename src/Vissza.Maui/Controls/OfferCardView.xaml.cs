using System.Globalization;
using System.Windows.Input;
using Vissza.Maui.Resources;
using Vissza.Shared.Dtos;
using Vissza.Shared.Enums;

namespace Vissza.Maui.Controls;

public partial class OfferCardView : Border
{
    public static readonly BindableProperty OfferProperty =
        BindableProperty.Create(nameof(Offer), typeof(OfferDto), typeof(OfferCardView),
            propertyChanged: OnOfferChanged);

    public static readonly BindableProperty ShowStatusProperty =
        BindableProperty.Create(nameof(ShowStatus), typeof(bool), typeof(OfferCardView), false);

    public static readonly BindableProperty TapCommandProperty =
        BindableProperty.Create(nameof(TapCommand), typeof(ICommand), typeof(OfferCardView));

    public OfferCardView() => InitializeComponent();

    public OfferDto? Offer
    {
        get => (OfferDto?)GetValue(OfferProperty);
        set => SetValue(OfferProperty, value);
    }

    public bool ShowStatus
    {
        get => (bool)GetValue(ShowStatusProperty);
        set => SetValue(ShowStatusProperty, value);
    }

    public ICommand? TapCommand
    {
        get => (ICommand?)GetValue(TapCommandProperty);
        set => SetValue(TapCommandProperty, value);
    }

    // A levezetett értékek a code-behindban vannak, nem konverterekben: így
    // egy helyen látszik, mit mutat a kártya, és nem kell hét konvertert
    // regisztrálni egyetlen listaelemhez.

    public string BottleTypeKind =>
        Offer is null ? "neutral" : DomainLabels.BottleTypeBadgeKind(Offer.BottleType);

    public string BottleTypeLabel =>
        Offer is null ? string.Empty : DomainLabels.BottleTypeShort(Offer.BottleType);

    public string StatusKind =>
        Offer is null ? "neutral" : DomainLabels.StatusBadgeKind(Offer.Status);

    public string StatusLabel =>
        Offer is null ? string.Empty : DomainLabels.OfferStatus(Offer.Status);

    public bool HasDonor => !string.IsNullOrEmpty(Offer?.DonorName);

    // A 0,00 átlag azt jelenti, hogy még senki nem értékelte - ilyenkor a
    // csillag elhagyása őszintébb, mint egy nulla csillagos értékelés.
    public bool HasDonorRating => Offer?.DonorRating is > 0;

    public string DonorRatingText =>
        Offer?.DonorRating is { } rating
            ? $"★ {rating.ToString("0.0", CultureInfo.CurrentCulture)}"
            : string.Empty;

    public bool ShowCollector =>
        Offer?.Status == OfferStatus.Reserved && !string.IsNullOrEmpty(Offer.SelectedCollectorName);

    public string CollectorText
    {
        get
        {
            if (Offer is null)
                return string.Empty;

            var rating = Offer.SelectedCollectorRating is > 0
                ? $"  ★ {Offer.SelectedCollectorRating.Value.ToString("0.0", CultureInfo.CurrentCulture)}"
                : string.Empty;

            return $"Gyűjtő: {Offer.SelectedCollectorName}{rating}";
        }
    }

    public bool ShowOtherDescription =>
        Offer?.BottleType == BottleType.Other && !string.IsNullOrEmpty(Offer.OtherDescription);

    public string EstimatedValueText =>
        Offer is null ? string.Empty : $"~{DomainLabels.EstimatedValue(Offer.Quantity):N0} Ft";

    static void OnOfferChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var card = (OfferCardView)bindable;

        // A levezetett tulajdonságok mind az Offer-ből számolnak, ezért az
        // egész csoportot újra kell értékeltetni, amikor az megváltozik.
        foreach (var name in DerivedProperties)
            card.OnPropertyChanged(name);
    }

    static readonly string[] DerivedProperties =
    [
        nameof(BottleTypeKind), nameof(BottleTypeLabel), nameof(StatusKind), nameof(StatusLabel),
        nameof(HasDonor), nameof(HasDonorRating), nameof(DonorRatingText),
        nameof(ShowCollector), nameof(CollectorText),
        nameof(ShowOtherDescription), nameof(EstimatedValueText)
    ];

    void OnTapped(object? sender, TappedEventArgs e)
    {
        if (Offer is not null && TapCommand?.CanExecute(Offer) == true)
            TapCommand.Execute(Offer);
    }
}
