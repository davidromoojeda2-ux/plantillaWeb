using System.Text.Json;

namespace Infrastructure.Serialization;

/// <summary>Opciones de serialización compartidas por los clientes externos.</summary>
internal static class JsonDefaults
{
    /// <summary>Deserialización tolerante a mayúsculas/minúsculas en los nombres de propiedad.</summary>
    public static readonly JsonSerializerOptions CaseInsensitive = new()
    {
        PropertyNameCaseInsensitive = true
    };
}
