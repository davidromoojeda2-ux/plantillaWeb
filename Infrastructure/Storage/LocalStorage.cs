using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Storage;

/// <summary>
/// Proveedor de almacenamiento local para desarrollo y servidores VPS.
/// Guarda archivos en wwwroot/uploads o en una ruta física configurable en el VPS.
/// </summary>
public class LocalStorage : IImageStorage
{
    private readonly string _basePath;
    private readonly string _baseUrl;

    public string BasePath => _basePath;
    public string BaseUrl => _baseUrl;

    public LocalStorage(IWebHostEnvironment env, IConfiguration config)
    {
        var configuredPath = config["LocalStorage:BasePath"];
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            _basePath = Path.IsPathRooted(configuredPath)
                ? configuredPath
                : Path.Combine(env.ContentRootPath, configuredPath);
        }
        else
        {
            var webRoot = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
            _basePath = Path.Combine(webRoot, "uploads");
        }

        _baseUrl = (config["LocalStorage:BaseUrl"] ?? "/uploads").TrimEnd('/');
        Directory.CreateDirectory(_basePath);
    }

    public async Task UploadAsync(string key, Stream stream, string contentType, CancellationToken ct = default)
    {
        var relativePath = key.Replace('/', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar);
        var filePath = Path.Combine(_basePath, relativePath);
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        await using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);
        await stream.CopyToAsync(fs, ct);
    }

    public Task DeleteAsync(string key, CancellationToken ct = default)
    {
        var relativePath = key.Replace('/', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar);
        var filePath = Path.Combine(_basePath, relativePath);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
        return Task.CompletedTask;
    }

    public string GetPublicUrl(string key)
    {
        var cleanKey = key.TrimStart('/');
        return $"{_baseUrl}/{cleanKey}";
    }
}
