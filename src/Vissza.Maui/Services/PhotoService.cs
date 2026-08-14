using Refit;

namespace Vissza.Maui.Services;

/// <summary>
/// A képválasztás nem hiba, csak nem sikerült: megtagadott jogosultság, vagy
/// olyan eszköz, amin nincs kamera. Külön típus, hogy az ApiErrors a saját
/// magyar üzenetét adhassa vissza a nyers kivételszöveg helyett.
/// </summary>
public sealed class PhotoPickException(string message) : Exception(message);

public enum PhotoChoice
{
    /// <summary>A felhasználó elvetette a választót.</summary>
    Cancelled,

    Uploaded,

    /// <summary>A meglévő kép törlését kérte.</summary>
    Removed
}

/// <summary>
/// A választó eredménye. Az útvonal relatív (/uploads/kep.jpg) - pontosan az,
/// amit az API-nak vissza kell küldeni.
/// </summary>
public readonly record struct PhotoResult(PhotoChoice Choice, string? Path);

/// <summary>
/// Fénykép választása és feltöltése. A profilkép és a felajánlás fotója
/// ugyanezt az utat járja, ezért egy helyen van.
/// </summary>
public sealed class PhotoService(IServiceProvider services)
{
    IVisszaApi Api => services.GetRequiredService<IVisszaApi>();

    /// <summary>
    /// Forrásválasztó, majd feltöltés. A törlést csak ott kínáljuk fel, ahol
    /// van mit törölni - egy üres kép törlése értelmetlen menüpont lenne.
    /// </summary>
    public async Task<PhotoResult> ChooseAsync(string title, bool allowRemove)
    {
        var source = allowRemove
            ? await Shell.Current.DisplayActionSheetAsync(title, "Mégsem", "Törlés", "Kamera", "Galéria")
            : await Shell.Current.DisplayActionSheetAsync(title, "Mégsem", null, "Kamera", "Galéria");

        if (source == "Törlés")
            return new PhotoResult(PhotoChoice.Removed, null);

        var file = await PickAsync(source);

        if (file is null)
            return new PhotoResult(PhotoChoice.Cancelled, null);

        await using var stream = await file.OpenReadAsync();

        var upload = await Api.UploadAsync(
            new StreamPart(stream, file.FileName, file.ContentType ?? "image/jpeg"));

        return new PhotoResult(PhotoChoice.Uploaded, upload.Url);
    }

    static async Task<FileResult?> PickAsync(string? source)
    {
        try
        {
            return source switch
            {
                "Kamera" => await MediaPicker.Default.CapturePhotoAsync(),

                // Csak a többes választás nem elavult; egyre korlátozzuk, és
                // az elsőt vesszük.
                "Galéria" => (await MediaPicker.Default.PickPhotosAsync(
                    new MediaPickerOptions { SelectionLimit = 1 })).FirstOrDefault(),

                // "Mégsem", vagy a lap elvetése - nem hiba.
                _ => null
            };
        }
        catch (Exception ex)
        {
            throw new PhotoPickException($"A kép kiválasztása nem sikerült: {ex.Message}");
        }
    }
}
