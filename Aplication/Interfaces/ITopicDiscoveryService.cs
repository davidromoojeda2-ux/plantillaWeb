using Aplication.DTOs.Automation;

namespace Aplication.Interfaces;

/// <summary>Solicitud de descubrimiento de temáticas.</summary>
public sealed class TopicDiscoveryRequest
{
    public List<string> Topics { get; set; } = [];
    public string Country { get; set; } = "us";
    public string Language { get; set; } = "es";
    public int Limit { get; set; } = 10;

    /// <summary>Clave de Serper, provista por el orquestador desde la configuración.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Número de resultados por consulta.</summary>
    public int Num { get; set; } = 10;

    /// <summary>Ventana temporal (tbs): "", qdr:d, qdr:w, qdr:m, qdr:y.</summary>
    public string Recency { get; set; } = "qdr:d";

    /// <summary>Tipo de búsqueda: "news" o "search".</summary>
    public string SearchType { get; set; } = "news";

    /// <summary>Búsqueda segura: "off" o "active".</summary>
    public string Safe { get; set; } = "off";

    /// <summary>Activar autocorrección.</summary>
    public bool Autocorrect { get; set; } = true;
}

/// <summary>Descubre temáticas candidatas a partir de un proveedor de búsqueda.</summary>
public interface ITopicDiscoveryService
{
    Task<List<DiscoveredTopic>> DiscoverAsync(TopicDiscoveryRequest request, CancellationToken ct = default);
}
