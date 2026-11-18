using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Aplication.Configuration;
using Domine.Exceptions;
using Microsoft.Extensions.Options;

namespace Aplication.Services;

/// <summary>Resultado normalizado de una validación de documento.</summary>
public sealed record ValidatedDocument(
    string OriginalFileName,
    string DisplayName,
    string Extension,
    string ContentType,
    long SizeBytes,
    string Sha256,
    string StorageSlug);

/// <summary>
/// Valida extensión, tamaño y firma real de un documento antes de almacenarlo.
/// No sustituye un antivirus, pero impide que un archivo activo o con extensión
/// falsa llegue al almacenamiento.
/// </summary>
public sealed class DocumentValidator
{
    private readonly DocumentsOptions _options;

    public DocumentValidator(IOptions<DocumentsOptions> options)
    {
        _options = options.Value;
    }

    public DocumentValidator(DocumentsOptions options) : this(Options.Create(options))
    {
    }

    public ValidatedDocument Validate(
        string originalFileName,
        string? displayName,
        MemoryStream content)
    {
        if (content is null || content.Length == 0)
            throw new DomainValidationException("El archivo está vacío.", nameof(originalFileName));

        if (content.Length > _options.MaxSizeBytes)
        {
            var maxMb = _options.MaxSizeBytes / (1024d * 1024d);
            throw new DomainValidationException(
                $"El archivo supera el tamaño máximo permitido ({maxMb:0.#} MB).",
                nameof(originalFileName));
        }

        var safeOriginalName = SanitizeFileName(originalFileName);
        var extension = DocumentsOptions.NormalizeExtension(Path.GetExtension(safeOriginalName));
        if (string.IsNullOrWhiteSpace(extension) || extension == ".")
            throw new DomainValidationException("El archivo debe tener una extensión.", nameof(originalFileName));

        if (DocumentsOptions.ForbiddenExtensions.Contains(extension))
            throw new DomainValidationException(
                $"El formato '{extension}' no está permitido por motivos de seguridad.",
                nameof(originalFileName));

        var allowed = _options.NormalizedExtensions();
        if (!allowed.Contains(extension))
            throw new DomainValidationException(
                $"El formato '{extension}' no está permitido. Formatos admitidos: {string.Join(", ", allowed.OrderBy(e => e))}.",
                nameof(originalFileName));

        var head = ReadHead(content);
        if (!HasValidSignature(extension, head))
            throw new DomainValidationException(
                "El contenido del archivo no coincide con el formato declarado.",
                nameof(originalFileName));

        var sha256 = ComputeSha256(content);
        var effectiveDisplayName = SanitizeDisplayName(displayName, safeOriginalName);

        return new ValidatedDocument(
            safeOriginalName,
            effectiveDisplayName,
            extension,
            ResolveContentType(extension),
            content.Length,
            sha256,
            BuildStorageSlug(effectiveDisplayName));
    }

    public static string SanitizeFileName(string fileName)
    {
        var name = Path.GetFileName((fileName ?? string.Empty).Trim());
        if (string.IsNullOrWhiteSpace(name)) return "documento";

        var invalid = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(name.Length);
        foreach (var character in name)
        {
            if (character is '\r' or '\n' or '\t' || invalid.Contains(character))
                builder.Append('-');
            else
                builder.Append(character);
        }

        var cleaned = Regex.Replace(builder.ToString(), @"\s+", " ").Trim();
        cleaned = Regex.Replace(cleaned, @"-{2,}", "-");
        if (cleaned.Length == 0) return "documento";
        if (cleaned.Length > 180)
        {
            var ext = Path.GetExtension(cleaned);
            cleaned = cleaned[..(180 - ext.Length)] + ext;
        }

        return cleaned;
    }

    public static string SanitizeDisplayName(string? displayName, string fallbackOriginalName)
    {
        var candidate = string.IsNullOrWhiteSpace(displayName)
            ? Path.GetFileNameWithoutExtension(fallbackOriginalName)
            : displayName.Trim();

        candidate = Path.GetFileName(candidate);
        candidate = Regex.Replace(candidate, @"[\u0000-\u001F\u007F]", " ");
        candidate = Regex.Replace(candidate, @"\s+", " ").Trim();

        if (candidate.Length == 0)
            candidate = Path.GetFileNameWithoutExtension(fallbackOriginalName);

        return candidate.Length > 200 ? candidate[..200].Trim() : candidate;
    }

    public static string BuildStorageSlug(string displayName)
    {
        var normalized = (displayName ?? string.Empty).Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                builder.Append(character);
        }

        var slug = Regex.Replace(builder.ToString(), @"[^a-z0-9_-]+", "-");
        slug = Regex.Replace(slug, @"-+", "-").Trim('-');
        return string.IsNullOrWhiteSpace(slug) ? "documento" : slug;
    }

    public static string ResolveContentType(string extension) => extension.ToLowerInvariant() switch
    {
        ".pdf" => "application/pdf",
        ".doc" => "application/msword",
        ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        ".xls" => "application/vnd.ms-excel",
        ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        ".ppt" => "application/vnd.ms-powerpoint",
        ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        ".txt" => "text/plain; charset=utf-8",
        ".csv" => "text/csv; charset=utf-8",
        ".rtf" => "application/rtf",
        ".odt" => "application/vnd.oasis.opendocument.text",
        ".ods" => "application/vnd.oasis.opendocument.spreadsheet",
        ".odp" => "application/vnd.oasis.opendocument.presentation",
        ".zip" => "application/zip",
        _ => "application/octet-stream"
    };

    private static byte[] ReadHead(MemoryStream content)
    {
        var length = (int)Math.Min(8192, content.Length);
        var head = new byte[length];
        content.Position = 0;
        var read = content.Read(head, 0, length);
        content.Position = 0;
        return read == length ? head : head[..read];
    }

    private static bool HasValidSignature(string extension, ReadOnlySpan<byte> head)
    {
        return extension.ToLowerInvariant() switch
        {
            ".pdf" => StartsWith(head, "%PDF-"u8),
            ".zip" or ".docx" or ".xlsx" or ".pptx" or ".odt" or ".ods" or ".odp" =>
                StartsWith(head, [0x50, 0x4B, 0x03, 0x04]) ||
                StartsWith(head, [0x50, 0x4B, 0x05, 0x06]) ||
                StartsWith(head, [0x50, 0x4B, 0x07, 0x08]),
            ".doc" or ".xls" or ".ppt" =>
                StartsWith(head, [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1]),
            ".rtf" => StartsWith(head, "{\\rtf"u8),
            ".txt" or ".csv" => IsSafeText(head),
            _ => false
        };
    }

    private static bool StartsWith(ReadOnlySpan<byte> bytes, ReadOnlySpan<byte> prefix) =>
        bytes.Length >= prefix.Length && bytes[..prefix.Length].SequenceEqual(prefix);

    private static bool IsSafeText(ReadOnlySpan<byte> head)
    {
        if (head.Contains((byte)0)) return false;

        var text = Encoding.UTF8.GetString(head).TrimStart('\uFEFF', ' ', '\t', '\r', '\n').ToLowerInvariant();
        return !text.StartsWith("<html", StringComparison.Ordinal) &&
               !text.StartsWith("<!doctype", StringComparison.Ordinal) &&
               !text.StartsWith("<script", StringComparison.Ordinal) &&
               !text.StartsWith("<svg", StringComparison.Ordinal) &&
               !text.StartsWith("<?xml", StringComparison.Ordinal);
    }

    private static string ComputeSha256(MemoryStream content)
    {
        content.Position = 0;
        var hash = SHA256.HashData(content);
        content.Position = 0;
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
