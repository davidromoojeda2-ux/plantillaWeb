using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Storage;

/// <summary>
/// Proveedor de almacenamiento local para desarrollo. Guarda archivos en wwwroot/uploads.
/// No requiere credenciales externas ni conexión de red.
/// </summary>
public class LocalStorage : IImageStorage
{
    private readonly string _basePath;
    private readonly string _baseUrl;

    public LocalStorage(IWebHostEnvironment env, IConfiguration config)
    {
        _basePath = Path.Combine(env.WebRootPath, "uploads");
        _baseUrl = config["LocalStorage:BaseUrl"] ?? "/uploads";
        Directory.CreateDirectory(_basePath);
    }

    public async Task UploadAsync(string key, Stream stream, string contentType, CancellationToken ct = default)
    {
        var filePath = Path.Combine(_basePath, key.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

        await using var fs = File.Create(filePath);
        await stream.CopyToAsync(fs, ct);
    }

    public Task DeleteAsync(string key, CancellationToken ct = default)
    {
        var filePath = Path.Combine(_basePath, key.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(filePath)) File.Delete(filePath);
        return Task.CompletedTask;
    }

    public string GetPublicUrl(string key) => $"{_baseUrl}/{key}";
}
