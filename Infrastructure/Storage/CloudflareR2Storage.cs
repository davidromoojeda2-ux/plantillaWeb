using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Storage;

/// <summary>
/// Proveedor de almacenamiento para Cloudflare R2 usando la API compatible con AWS S3.
/// Configurar en appsettings / User Secrets:
///   CloudflareR2:AccountId, CloudflareR2:AccessKeyId, CloudflareR2:SecretAccessKey,
///   CloudflareR2:BucketName, CloudflareR2:PublicDomain
/// </summary>
public class CloudflareR2Storage : IImageStorage
{
    private readonly AmazonS3Client _s3;
    private readonly string _bucket;
    private readonly string _publicDomain;

    public CloudflareR2Storage(IConfiguration config)
    {
        var accountId = config["CloudflareR2:AccountId"]!;
        var accessKey = config["CloudflareR2:AccessKeyId"]!;
        var secretKey = config["CloudflareR2:SecretAccessKey"]!;

        _bucket = config["CloudflareR2:BucketName"]!;
        _publicDomain = config["CloudflareR2:PublicDomain"]!.TrimEnd('/');

        var s3Config = new AmazonS3Config
        {
            ServiceURL = $"https://{accountId}.r2.cloudflarestorage.com",
            ForcePathStyle = true
        };

        _s3 = new AmazonS3Client(accessKey, secretKey, s3Config);
    }

    public async Task UploadAsync(string key, Stream stream, string contentType, CancellationToken ct = default)
    {
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
        var request = new DeleteObjectRequest
        {
            BucketName = _bucket,
            Key = key
        };
        await _s3.DeleteObjectAsync(request, ct);
    }

    public string GetPublicUrl(string key) => $"{_publicDomain}/{key}";
}
