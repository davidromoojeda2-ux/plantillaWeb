using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Storage;

/// <summary>
/// Proveedor de almacenamiento para Cloudflare R2 compatible con S3.
/// Hereda de S3Storage conservando retrocompatibilidad completa.
/// </summary>
public class CloudflareR2Storage : S3Storage
{
    public CloudflareR2Storage(IConfiguration config, ILogger<S3Storage>? logger = null)
        : base(config, logger)
    {
    }
}
