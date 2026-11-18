using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Html;

namespace paginaWeb.Seo;

/// <summary>
/// Serializa un grafo Schema.org y lo envuelve en una etiqueta
/// <c>&lt;script type="application/ld+json"&gt;</c> segura para incrustar en HTML.
/// </summary>
public static class JsonLd
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = false
    };

    /// <summary>
    /// Devuelve la etiqueta completa lista para renderizar con Razor. Escapa
    /// <c>&lt;/</c> para que ningún valor pueda cerrar el bloque <c>script</c>.
    /// </summary>
    public static HtmlString Render(object graph)
    {
        var json = JsonSerializer.Serialize(graph, SerializerOptions)
            .Replace("</", "<\\/")
            .Replace("\u2028", "\\u2028")
            .Replace("\u2029", "\\u2029");

        return new HtmlString($"<script type=\"application/ld+json\">{json}</script>");
    }
}
