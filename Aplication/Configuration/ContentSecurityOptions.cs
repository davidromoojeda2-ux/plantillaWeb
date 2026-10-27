namespace Aplication.Configuration;

/// <summary>
/// Política y límites para el contenido editorial estructurado.
/// Se enlaza desde la sección <c>ContentSecurity</c> de la configuración.
/// </summary>
public sealed class ContentSecurityOptions
{
    public const string SectionName = "ContentSecurity";

    /// <summary>Número máximo de bloques que puede contener un cuerpo editorial.</summary>
    public int MaxBlocks { get; set; } = 100;

    /// <summary>Tamaño máximo, en bytes, del JSON enviado por el editor.</summary>
    public int MaxJsonBytes { get; set; } = 512 * 1024;

    /// <summary>Longitud máxima del HTML inline de un bloque de texto.</summary>
    public int MaxRichTextHtmlLength { get; set; } = 40_000;

    /// <summary>Longitud máxima del texto plano derivado de un bloque de texto.</summary>
    public int MaxTextLength { get; set; } = 20_000;

    /// <summary>Cantidad máxima de elementos en una lista.</summary>
    public int MaxListItems { get; set; } = 50;

    /// <summary>Cantidad máxima de filas de una tabla (incluida la cabecera).</summary>
    public int MaxTableRows { get; set; } = 50;

    /// <summary>Cantidad máxima de columnas de una tabla.</summary>
    public int MaxTableColumns { get; set; } = 10;

    /// <summary>Longitud máxima permitida para una URL.</summary>
    public int MaxUrlLength { get; set; } = 2048;

    /// <summary>Hosts externos autorizados para imágenes del cuerpo.</summary>
    public List<string> AllowedImageHosts { get; set; } = [];
}
