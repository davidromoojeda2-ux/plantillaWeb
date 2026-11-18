using Aplication.DTOs;
using Microsoft.Net.Http.Headers;

namespace paginaWeb.Helpers;

/// <summary>
/// Escribe una descarga de documento con cabeceras seguras, disposición de
/// adjunto y soporte de un único rango HTTP.
/// </summary>
public static class DocumentDownloadWriter
{
    public static bool TryParseRange(
        string? rangeHeader,
        long totalBytes,
        out long offset,
        out long length,
        out bool isPartial)
    {
        offset = 0;
        length = totalBytes;
        isPartial = false;

        if (string.IsNullOrWhiteSpace(rangeHeader))
            return true;

        var value = rangeHeader.Trim();
        if (!value.StartsWith("bytes=", StringComparison.OrdinalIgnoreCase))
            return false;

        var range = value["bytes=".Length..].Trim();
        if (range.Contains(',', StringComparison.Ordinal))
            return false;

        var parts = range.Split('-', 2);
        if (parts.Length != 2) return false;

        if (string.IsNullOrWhiteSpace(parts[0]))
        {
            if (!long.TryParse(parts[1], out var suffix) || suffix <= 0)
                return false;

            length = Math.Min(suffix, totalBytes);
            offset = Math.Max(0, totalBytes - length);
            isPartial = true;
            return length > 0;
        }

        if (!long.TryParse(parts[0], out var start) || start < 0 || start >= totalBytes)
            return false;

        var end = totalBytes - 1;
        if (!string.IsNullOrWhiteSpace(parts[1]))
        {
            if (!long.TryParse(parts[1], out end) || end < start)
                return false;

            end = Math.Min(end, totalBytes - 1);
        }

        offset = start;
        length = end - start + 1;
        isPartial = true;
        return length > 0;
    }

    public static async Task WriteAsync(
        HttpResponse response,
        DocumentDownloadResult document,
        long? offset,
        long? length,
        bool publicCache,
        CancellationToken ct)
    {
        var total = document.SizeBytes;
        var start = offset ?? 0;
        var count = length ?? total;
        var partial = offset.HasValue && length.HasValue;

        response.StatusCode = partial ? StatusCodes.Status206PartialContent : StatusCodes.Status200OK;
        response.ContentType = document.ContentType;
        response.ContentLength = count;
        response.Headers.AcceptRanges = "bytes";
        response.Headers.XContentTypeOptions = "nosniff";
        response.Headers.ETag = $"\"{document.Sha256}\"";
        response.Headers.LastModified = document.UploadedAt.ToUniversalTime().ToString("R");
        response.Headers.CacheControl = publicCache
            ? "public, max-age=86400"
            : "private, no-store";

        if (partial)
        {
            response.Headers.ContentRange = $"bytes {start}-{start + count - 1}/{total}";
        }

        var disposition = new ContentDispositionHeaderValue("attachment")
        {
            FileName = document.FileName,
            FileNameStar = document.FileName
        };
        response.Headers.ContentDisposition = disposition.ToString();

        try
        {
            await document.Stream.CopyToAsync(response.Body, ct);
        }
        finally
        {
            await document.Stream.DisposeAsync();
        }
    }
}
