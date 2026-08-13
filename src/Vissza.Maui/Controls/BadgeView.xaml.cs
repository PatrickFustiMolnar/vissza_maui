namespace Vissza.Maui.Controls;

public partial class BadgeView : Border
{
    public static readonly BindableProperty KindProperty =
        BindableProperty.Create(nameof(Kind), typeof(string), typeof(BadgeView), "neutral");

    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(BadgeView), string.Empty);

    public BadgeView() => InitializeComponent();

    /// <summary>
    /// A színkulcs: pet, glass, aluminum, other, active, reserved, neutral.
    /// Ismeretlen érték esetén egyik trigger sem fut, és a jelvény
    /// színezetlen marad - lásd DomainLabels.
    /// </summary>
    public string Kind
    {
        get => (string)GetValue(KindProperty);
        set => SetValue(KindProperty, value);
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }
}
