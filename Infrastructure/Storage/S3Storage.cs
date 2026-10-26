using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Storage;

/// <summary>
/// Proveedor de almacenamiento S3 compatible (AWS S3, Cloudflare R2, MinIO).
/// Verifica disponibilidad antes de operaciones y prioriza la nube frente al almacenamiento local.
/// </summary>
public class S3Storage : IImageStorage
{
    private readonly AmazonS3Client? _s3;
    private readonly string _bucket;
    private readonly string _publicDomain;
    private readonly bool _isConfigured;
    private readonly bool _isR2;
    private readonly ILogger<S3Storage>? _logger;

    // Cache para disponibilidad (TTL de 30 segundos)
    private bool? _cachedAvailability;
    private DateTime _lastCheckTime = DateTime.MinValue;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(30);

    public string BucketName => _bucket;
    public string PublicDomain => _publicDomain;
    public bool IsConfigured => _isConfigured;
    public string StorageType => _isR2 ? "r2" : "s3";

    public S3Storage(IConfiguration config, ILogger<S3Storage>? logger = null)
    {
        _logger = logger;

        // 1. Detección de Cloudflare R2
        var r2AccountId = config["CloudflareR2:AccountId"] ?? Environment.GetEnvironmentVariable("R2_ACCOUNT_ID");
        var r2AccessKey = config["CloudflareR2:AccessKeyId"] ?? Environment.GetEnvironmentVariable("R2_ACCESS_KEY_ID");
        var r2SecretKey = config["CloudflareR2:SecretAccessKey"] ?? Environment.GetEnvironmentVariable("R2_SECRET_ACCESS_KEY");
        var r2Bucket = config["CloudflareR2:BucketName"] ?? Environment.GetEnvironmentVariable("R2_BUCKET_NAME");
        var r2PublicDomain = config["CloudflareR2:PublicDomain"] ?? Environment.GetEnvironmentVariable("R2_PUBLIC_DOMAIN");

        // 2. Detección de AWS S3 o S3 genérico
        var s3AccessKey = config["S3:AccessKeyId"] ?? config["AWS:AccessKey"] ?? Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID");
        var s3SecretKey = config["S3:SecretAccessKey"] ?? config["AWS:SecretKey"] ?? Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY");
        var s3Bucket = config["S3:BucketName"] ?? config["AWS:BucketName"] ?? Environment.GetEnvironmentVariable("AWS_BUCKET_NAME");
        var s3ServiceUrl = config["S3:ServiceUrl"] ?? config["AWS:ServiceURL"] ?? Environment.GetEnvironmentVariable("S3_SERVICE_URL");
        var s3Region = config["S3:Region"] ?? config["AWS:Region"] ?? Environment.GetEnvironmentVariable("AWS_REGION") ?? "us-east-1";
        var s3PublicDomain = config["S3:PublicDomain"] ?? Environment.GetEnvironmentVariable("S3_PUBLIC_DOMAIN");

        if (!string.IsNullOrWhiteSpace(r2AccountId) && !string.IsNullOrWhiteSpace(r2AccessKey) && !string.IsNullOrWhiteSpace(r2SecretKey))
        {
            _isR2 = true;
            _bucket = !string.IsNullOrWhiteSpace(r2Bucket) ? r2Bucket : "portal-noticias";
            _publicDomain = (r2PublicDomain ?? string.Empty).TrimEnd('/');

            var s3Config = new AmazonS3Config
            {
                ServiceURL = $"https://{r2AccountId}.r2.cloudflarestorage.com",
                ForcePathStyle = true
            };

            _s3 = new AmazonS3Client(r2AccessKey, r2SecretKey, s3Config);
            _isConfigured = true;
            _logger?.LogInformation("S3Storage configurado usando Cloudflare R2 (Bucket: {Bucket}).", _bucket);
        }
        else if (!string.IsNullOrWhiteSpace(s3AccessKey) && !string.IsNullOrWhiteSpace(s3SecretKey))
        {
            _isR2 = false;
            _bucket = !string.IsNullOrWhiteSpace(s3Bucket) ? s3Bucket : "portal-noticias";
            _publicDomain = (s3PublicDomain ?? string.Empty).TrimEnd('/');

            var s3Config = new AmazonS3Config
            {
                ForcePathStyle = true
            };

            if (!string.IsNullOrWhiteSpace(s3ServiceUrl))
            {
                s3Config.ServiceURL = s3ServiceUrl;
            }
            else
            {
                s3Config.RegionEndpoint = RegionEndpoint.GetBySystemName(s3Region);
            }

            _s3 = new AmazonS3Client(s3AccessKey, s3SecretKey, s3Config);
            _isConfigured = true;
            _logger?.LogInformation("S3Storage configurado usando AWS S3 (Bucket: {Bucket}).", _bucket);
        }
        else
        {
            _isConfigured = false;
            _bucket = "portal-noticias";
            _publicDomain = string.Empty;
            _s3 = null;
            _logger?.LogWarning("Credenciales S3/Cloudflare R2 no configuradas. El servicio operará con almacenamiento local como fallback.");
        }
    }

    /// <summary>
    /// Verifica la disponibilidad real del servicio S3 con una sonda ligera y timeout controlado.
    /// </summary>
    public async Task<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        if (!_isConfigured || _s3 == null)
        {
            return false;
        }

        // Usar cache de disponibilidad para no ralentizar peticiones frecuentes
        if (_cachedAvailability.HasValue && (DateTime.UtcNow - _lastCheckTime) < CacheDuration)
        {
            return _cachedAvailability.Value;
        }

        try
        {
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

            // Verificamos conexión y existencia del bucket listando máximo 1 objeto
            var response = await _s3.ListObjectsV2Async(new ListObjectsV2Request
            {
                BucketName = _bucket,
                MaxKeys = 1
            }, linkedCts.Token);

            _cachedAvailability = response != null;
            _lastCheckTime = DateTime.UtcNow;
            return _cachedAvailability.Value;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning("Verificación de S3 fallida. El servicio S3 no responde o no está disponible: {Message}", ex.Message);
            _cachedAvailability = false;
            _lastCheckTime = DateTime.UtcNow;
            return false;
        }
    }

    public async Task UploadAsync(string key, Stream stream, string contentType, CancellationToken ct = default)
    {
        if (!_isConfigured || _s3 == null)
        {
            throw new InvalidOperationException("No se puede subir al servicio S3 porque no está configurado.");
        }

        var request = new PutObjectRequest
        {
            BucketName = _bucket,
            Key = key,
            InputStream = stream,
            ContentType = contentType,
            CannedACL = S3CannedACL.PublicRead
        };

        await _s3.PutObjectAsync(request, ct);
    }

    public async Task DeleteAsync(string key, CancellationToken ct = default)
    {
        if (!_isConfigured || _s3 == null) return;

        try
        {
            var request = new DeleteObjectRequest
            {
                BucketName = _bucket,
                Key = key
            };
            await _s3.DeleteObjectAsync(request, ct);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error al eliminar objeto '{Key}' del bucket '{Bucket}': {Message}", key, _bucket, ex.Message);
        }
    }

    public string GetPublicUrl(string key)
    {
        var cleanKey = key.TrimStart('/');
        if (!string.IsNullOrEmpty(_publicDomain))
        {
            return $"{_publicDomain}/{cleanKey}";
        }
        return $"/uploads/{cleanKey}";
    }
}
