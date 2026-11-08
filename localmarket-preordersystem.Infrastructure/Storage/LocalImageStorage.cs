using localmarket_preordersystem.Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace localmarket_preordersystem.Infrastructure.Storage;

public sealed class ImageStorageOptions
{
    public const string SectionName = "ImageStorage";

    /// <summary>A feltöltött termékképek gyökérmappája (az API ezt szolgálja ki statikus fájlként).</summary>
    public string RootPath { get; set; } = Path.Combine("uploads", "products");
}

/// <summary>Helyi fájlrendszeren tárolt képek. A fájlnév szerver oldalon generált GUID, a kliens nevéből semmi nem kerül az útvonalba.</summary>
internal sealed class LocalImageStorage : IImageStorage
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".png", ".webp" };
    private readonly string _root;

    public LocalImageStorage(IOptions<ImageStorageOptions> options)
    {
        _root = Path.GetFullPath(options.Value.RootPath);
        Directory.CreateDirectory(_root);
    }

    public async Task<string> SaveAsync(byte[] content, string extension, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        if (!AllowedExtensions.Contains(extension))
            throw new ArgumentException("Nem támogatott képkiterjesztés.", nameof(extension));

        var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        await File.WriteAllBytesAsync(Path.Combine(_root, fileName), content, cancellationToken);
        return fileName;
    }

    public Task DeleteAsync(string fileName, CancellationToken cancellationToken = default)
    {
        // útvonal-bejárás ellen: csak egyszerű fájlnév fogadható el
        if (string.IsNullOrWhiteSpace(fileName) || fileName != Path.GetFileName(fileName))
            return Task.CompletedTask;

        var path = Path.Combine(_root, fileName);
        if (File.Exists(path))
            File.Delete(path);
        return Task.CompletedTask;
    }
}
