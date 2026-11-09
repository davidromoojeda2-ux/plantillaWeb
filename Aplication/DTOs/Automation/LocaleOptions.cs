namespace Aplication.DTOs.Automation;

/// <summary>Opciones de país e idioma para los desplegables de configuración.</summary>
public static class LocaleOptions
{
    public static readonly IReadOnlyList<(string Code, string Name)> Countries =
    [
        ("us", "Estados Unidos"), ("mx", "México"), ("es", "España"), ("ar", "Argentina"),
        ("co", "Colombia"), ("cl", "Chile"), ("pe", "Perú"), ("ve", "Venezuela"),
        ("ec", "Ecuador"), ("uy", "Uruguay"), ("py", "Paraguay"), ("bo", "Bolivia"),
        ("cr", "Costa Rica"), ("pa", "Panamá"), ("do", "Rep. Dominicana"),
        ("gt", "Guatemala"), ("hn", "Honduras"), ("sv", "El Salvador"), ("ni", "Nicaragua"),
        ("br", "Brasil"), ("gb", "Reino Unido"), ("fr", "Francia"), ("de", "Alemania"),
        ("it", "Italia"), ("pt", "Portugal")
    ];

    public static readonly IReadOnlyList<(string Code, string Name)> Languages =
    [
        ("es", "Español"), ("en", "Inglés"), ("pt", "Portugués"),
        ("fr", "Francés"), ("de", "Alemán"), ("it", "Italiano")
    ];

    /// <summary>Devuelve el nombre legible del país a partir de su código (gl).</summary>
    public static string DescribeCountry(string? code)
    {
        var normalized = (code ?? string.Empty).Trim();
        var match = Countries.FirstOrDefault(c =>
            string.Equals(c.Code, normalized, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(match.Name)) return match.Name;
        return string.IsNullOrWhiteSpace(normalized) ? "Estados Unidos" : normalized;
    }
}
