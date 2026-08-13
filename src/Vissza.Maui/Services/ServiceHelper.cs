namespace Vissza.Maui.Services;

/// <summary>
/// Szolgáltatás-feloldás olyan helyekről, ahová a MAUI nem tud befecskendezni.
///
/// Konkrétan a Shell DataTemplate-jei: a Shell paraméter nélküli
/// konstruktorral példányosítja az oldalakat, tehát egy DI-konstruktoros
/// oldal ott nem jön létre - iOS-en ez natív összeomlásként jelentkezik,
/// nem beszédes kivételként.
/// </summary>
public static class ServiceHelper
{
    static IServiceProvider? _services;

    public static void Initialize(IServiceProvider services) => _services = services;

    public static T Get<T>() where T : notnull =>
        _services is null
            ? throw new InvalidOperationException("A ServiceHelper nincs inicializálva.")
            : _services.GetRequiredService<T>();
}
