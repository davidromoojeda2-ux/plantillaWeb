using Aplication.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Storage;

/// <summary>
/// Almacenamiento local de documentos. Por defecto usa "storage/documents"
/// dentro del ContentRoot, fuera de wwwroot, para que los archivos nunca se
/// sirvan directamente por el middleware estático.
/// </summary>
public class LocalDocumentStorage : IDocumentStorage
{
    private readonly string _basePath;
    private readonly ILogger<LocalDocumentStorage>? _logger;

    public LocalDocumentStorage(
        IWebHostEnvironment env,
        IConfiguration config,
        IOptions<DocumentsOptions> options,
        ILogger<LocalDocumentStorage>? logger = null)
    {
        _logger = logger;

        var configuredPath = config["Documents:LocalPath"];
        if (string.IsNullOrWhiteSpace(configuredPath))
            configuredPath = options.Value.LocalPath;
        if (string.IsNullOrWhiteSpace(configuredPath))
            configuredPath = "storage/documents";

        _basePath = Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.Combine(env.ContentRootPath, configuredPath);

        Directory.CreateDirectory(_basePath);
    }

    public string StorageType => "local";

    public string BasePath => _basePath;

    public async Task UploadAsync(string key, Stream stream, string contentType, CancellationToken ct = default)
    {
        var filePath = ResolvePath(key);
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        await using var file = new FileStream(
            filePath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            81920,
            useAsync: true);

        await stream.CopyToAsync(file, ct);
    }

    public Task<Stream?> OpenReadAsync(string key, long? offset = null, long? length = null, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var filePath = ResolvePath(key);
        if (!File.Exists(filePath))
            return Task.FromResult<Stream?>(null);

        var file = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            81920,
            useAsync: true);

        if (offset.HasValue)
        {
            if (offset.Value < 0 || offset.Value >= file.Length)
            {
                file.Dispose();
                return Task.FromResult<Stream?>(null);
            }

            file.Seek(offset.Value, SeekOrigin.Begin);
            if (length.HasValue)
                return Task.FromResult<Stream?>(new BoundedReadStream(file, length.Value));
        }

        return Task.FromResult<Stream?>(file);
    }

    public Task DeleteAsync(string key, CancellationToken ct = default)
    {
        var filePath = ResolvePath(key);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        return Task.CompletedTask;
    }

    public Task<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        try
        {
            Directory.CreateDirectory(_basePath);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "El almacenamiento local de documentos no está disponible: {Message}", ex.Message);
            return Task.FromResult(false);
        }
    }

    private string ResolvePath(string key)
    {
        var relative = key.Replace('/', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(_basePath, relative));
        var root = Path.GetFullPath(_basePath).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

        if (!fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("La clave del documento intenta salir del almacenamiento local.");

        return fullPath;
    }
}
