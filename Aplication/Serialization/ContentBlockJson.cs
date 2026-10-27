using System.Text.Json;
using Aplication.DTOs.ContentBlocks;

namespace Aplication.Serialization;

/// <summary>
/// Contrato JSON estricto para el cuerpo editorial del editor administrativo.
/// </summary>
public static class ContentBlockJson
{
    public static JsonSerializerOptions Options { get; } = CreateOptions();

    private static JsonSerializerOptions CreateOptions() => new(JsonSerializerDefaults.Web)
    {
        AllowTrailingCommas = false,
        ReadCommentHandling = JsonCommentHandling.Disallow,
        PropertyNameCaseInsensitive = false,
        MaxDepth = 32,
        NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.Strict
    };

    public static string Serialize(IEnumerable<ContentBlockDto>? blocks) =>
        JsonSerializer.Serialize(blocks ?? [], Options);

    public static List<ContentBlockDto> Deserialize(string? json, int maxBytes = 512 * 1024)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new JsonException("El cuerpo del contenido es obligatorio.");

        // UTF-8 puede ocupar hasta 3 bytes por carácter; el límite exacto se
        // comprueba también sobre el texto para evitar payloads desmesurados.
        if (System.Text.Encoding.UTF8.GetByteCount(json) > maxBytes)
            throw new JsonException($"El cuerpo del contenido supera el máximo de {maxBytes} bytes.");

        var blocks = JsonSerializer.Deserialize<List<ContentBlockDto>>(json, Options)
            ?? throw new JsonException("El cuerpo del contenido no es válido.");

        return blocks;
    }
}
