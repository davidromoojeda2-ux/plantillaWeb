namespace Domine.Content;

/// <summary>
/// Límites por defecto del contenido editorial estructurado. La capa de aplicación
/// puede reforzarlos mediante la configuración de seguridad.
/// </summary>
public static class ContentLimits
{
    public const int CurrentSchemaVersion = 1;

    public const int MaxBlocks = 100;
    public const int MaxRichTextHtmlLength = 40_000;
    public const int MaxTextLength = 20_000;
    public const int MaxListItems = 50;
    public const int MaxTableRows = 50;
    public const int MaxTableColumns = 10;
    public const int MaxUrlLength = 2_048;
}
