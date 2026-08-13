using Microsoft.AspNetCore.Mvc;
using Vissza.Shared.Dtos;

namespace Vissza.Api.Endpoints;

public static class UploadEndpoints
{
    const long MaxBytes = 5 * 1024 * 1024;

    /// <summary>
    /// Engedélyezett képformátumok: kiterjesztés, MIME típus és a fájl első
    /// bájtjai. A kiterjesztés és a MIME típus a klienstől jön, tehát
    /// hamisítható - a magic bytes nem.
    /// </summary>
    static readonly (string Extension, string Mime, byte[] Magic)[] AllowedImages =
    [
        (".jpg",  "image/jpeg", [0xFF, 0xD8, 0xFF]),
        (".jpeg", "image/jpeg", [0xFF, 0xD8, 0xFF]),
        (".png",  "image/png",  [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]),
        (".gif",  "image/gif",  [0x47, 0x49, 0x46, 0x38])
    ];

    public static void MapUploadEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGroup("/api/upload")
            .RequireAuthorization()
            .MapPost("/", UploadAsync)
            .DisableAntiforgery();
    }

    static async Task<IResult> UploadAsync(
        [FromForm] IFormFile? file,
        IWebHostEnvironment environment,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return Results.BadRequest(new MessageResponse("No file uploaded"));

        if (file.Length > MaxBytes)
            return Results.BadRequest(new MessageResponse("File is too large (max 5MB)"));

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        var allowed = AllowedImages.FirstOrDefault(a =>
            a.Extension == extension
            && string.Equals(a.Mime, file.ContentType, StringComparison.OrdinalIgnoreCase));

        if (allowed.Extension is null)
        {
            return Results.BadRequest(new MessageResponse(
                "Only image files are allowed (jpeg, jpg, png, gif)"));
        }

        await using var stream = file.OpenReadStream();

        if (!await HasMagicBytesAsync(stream, allowed.Magic, ct))
        {
            return Results.BadRequest(new MessageResponse(
                "Only image files are allowed (jpeg, jpg, png, gif)"));
        }

        // A fájlnevet mi állítjuk elő, a feltöltöttet sosem használjuk -
        // így nem lehet vele könyvtárból kilépni, és nem is ütközhet.
        var name = $"file-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}"
                   + $"-{Random.Shared.Next(100_000_000, 999_999_999)}{extension}";

        var directory = Path.Combine(environment.ContentRootPath, "uploads");
        Directory.CreateDirectory(directory);

        await using (var target = File.Create(Path.Combine(directory, name)))
        {
            stream.Position = 0;
            await stream.CopyToAsync(target, ct);
        }

        // Relatív útvonal megy vissza, ahogy a régi API-nál: az abszolutizálás
        // az ImageUrlService dolga, olvasáskor.
        return Results.Ok(new UploadResponse
        {
            Message = "File uploaded successfully",
            Url = $"/uploads/{name}",
            Filename = name
        });
    }

    static async Task<bool> HasMagicBytesAsync(Stream stream, byte[] magic, CancellationToken ct)
    {
        var head = new byte[magic.Length];
        var read = await stream.ReadAtLeastAsync(head, magic.Length, throwOnEndOfStream: false, ct);

        return read == magic.Length && head.AsSpan().SequenceEqual(magic);
    }
}
