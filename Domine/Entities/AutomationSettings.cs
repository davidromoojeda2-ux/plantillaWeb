using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Domine.Entities;

/// <summary>
/// Configuración durable del pipeline de automatización. Se persiste como documento único.
/// </summary>
[BsonIgnoreExtraElements]
public class AutomationSettings
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    /// <summary>Si es false, la automatización no genera artículos (apagado global).</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Si es true, los artículos se publican directamente; si false quedan en borrador.</summary>
    public bool AutoPublish { get; set; } = false;

    /// <summary>Pausa las corridas programadas (no impide la ejecución manual).</summary>
    public bool Paused { get; set; } = false;

    /// <summary>Frecuencia de corridas programadas en minutos.</summary>
    public int FrequencyMinutes { get; set; } = 360;

    /// <summary>Ids de categorías habilitadas para la automatización.</summary>
    public List<string> CategoryIds { get; set; } = [];

    /// <summary>Temáticas base consultadas en Serper.</summary>
    public List<string> BaseTopics { get; set; } = [];

    /// <summary>País (código gl) para las consultas de Serper.</summary>
    public string Country { get; set; } = "us";

    /// <summary>Idioma (código hl) para las consultas y la generación.</summary>
    public string Language { get; set; } = "es";

    /// <summary>Máximo de artículos a generar por corrida.</summary>
    public int MaxArticlesPerRun { get; set; } = 5;

    /// <summary>Máximo de tokens solicitados a la IA por artículo.</summary>
    public int MaxTokensPerArticle { get; set; } = 8000;

    /// <summary>Clave de Serper (api.serper.dev).</summary>
    public string SerperApiKey { get; set; } = string.Empty;

    /// <summary>Tipo de búsqueda en Serper: "news" (noticias) o "search" (web).</summary>
    public string SerperType { get; set; } = "news";

    /// <summary>Ventana temporal (tbs): "" cualquiera, qdr:d 24 h, qdr:w semana, qdr:m mes, qdr:y año.</summary>
    public string SerperRecency { get; set; } = "qdr:d";

    /// <summary>Número de resultados por consulta.</summary>
    public int SerperNum { get; set; } = 10;

    /// <summary>Búsqueda segura: "off" o "active".</summary>
    public string SerperSafe { get; set; } = "off";

    /// <summary>Activar autocorrección de Serper.</summary>
    public bool SerperAutocorrect { get; set; } = true;

    /// <summary>Clave del proveedor de IA.</summary>
    public string AiApiKey { get; set; } = string.Empty;

    /// <summary>Identificador del preset de proveedor de IA (openai, deepseek, etc.).</summary>
    public string AiProvider { get; set; } = "openai";

    /// <summary>URL base del endpoint de IA.</summary>
    public string AiBaseUrl { get; set; } = "https://api.openai.com/v1";

    /// <summary>Modelo de IA.</summary>
    public string AiModel { get; set; } = "gpt-4o-mini";

    /// <summary>Si es true, los artículos generados se programan en lugar de publicarse de inmediato.</summary>
    public bool ScheduleEnabled { get; set; } = false;

    /// <summary>Hora (0-23) del horario por defecto de publicación.</summary>
    public int PublishHour { get; set; } = 9;

    /// <summary>Minuto (0-59) del horario por defecto de publicación.</summary>
    public int PublishMinute { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
