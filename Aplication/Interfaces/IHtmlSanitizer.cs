namespace Aplication.Interfaces;

/// <summary>
/// Sanitiza fragmentos HTML inline y extrae su representación de texto plano.
/// </summary>
public interface IHtmlSanitizer
{
    string Sanitize(string? html);
    string ToPlainText(string? html);
}
