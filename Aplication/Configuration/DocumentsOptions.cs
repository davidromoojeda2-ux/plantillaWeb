namespace Aplication.Configuration;

/// <summary>
/// Configuración de la biblioteca de documentos. Los valores se enlazan desde
/// la sección "Documents" de appsettings y pueden ajustarse por entorno.
/// </summary>
public sealed class DocumentsOptions
{
    public const string SectionName = "Documents";

    public const long DefaultMaxSizeBytes = 25 * 1024 * 1024;

    public List<string> AllowedExtensions { get; set; } =
    [
        ".pdf",
        ".doc",
        ".docx",
        ".xls",
        ".xlsx",
        ".ppt",
        ".pptx",
        ".txt",
        ".csv",
        ".rtf",
        ".odt",
        ".ods",
        ".odp"
    ];

    public long MaxSizeBytes { get; set; } = DefaultMaxSizeBytes;

    public int MaxPerArticle { get; set; } = 10;

    /// <summary>Ruta local para documentos. Se recomienda fuera de wwwroot.</summary>
    public string LocalPath { get; set; } = "storage/documents";

    /// <summary>Extensiones activas normalizadas y sin duplicados.</summary>
    public HashSet<string> NormalizedExtensions()
    {
        return AllowedExtensions
            .Where(extension => !string.IsNullOrWhiteSpace(extension))
            .Select(NormalizeExtension)
            .Where(extension => extension.Length > 1)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public static string NormalizeExtension(string extension)
    {
        var value = (extension ?? string.Empty).Trim().ToLowerInvariant();
        if (value.Length == 0) return value;
        return value.StartsWith('.') ? value : "." + value;
    }

    /// <summary>
    /// Extensiones que nunca se permiten, incluso si un operador las añade
    /// accidentalmente a la configuración.
    /// </summary>
    public static readonly HashSet<string> ForbiddenExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".html", ".htm", ".xhtml", ".svg", ".xml", ".js", ".mjs", ".cjs",
        ".exe", ".dll", ".com", ".scr", ".msi", ".bat", ".cmd", ".ps1",
        ".sh", ".bash", ".php", ".jsp", ".aspx", ".asp", ".cgi", ".jar",
        ".apk", ".vbs", ".wsf", ".hta", ".reg", ".lnk"
    };
}
