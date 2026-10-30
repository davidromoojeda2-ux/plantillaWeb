using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Domine.Entities;

/// <summary>Estado de una corrida del pipeline de automatización.</summary>
public enum AutomationRunStatus
{
    EnProgreso,
    Completado,
    Fallido
}

/// <summary>Origen de disparo de una corrida de automatización.</summary>
public enum AutomationTrigger
{
    Programada,
    Manual
}

/// <summary>Estado de una decisión editorial sobre una temática repetida.</summary>
public enum AutomationDecisionStatus
{
    Pendiente,
    Continuado,
    Omitido
}

/// <summary>
/// Registro de una ejecución del pipeline de automatización (descubrir → generar → publicar).
/// </summary>
[BsonIgnoreExtraElements]
public class AutomationRun
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    public AutomationRunStatus Status { get; set; } = AutomationRunStatus.EnProgreso;
    public AutomationTrigger Trigger { get; set; } = AutomationTrigger.Programada;

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? FinishedAt { get; set; }

    /// <summary>Total de temáticas descubiertas en la corrida.</summary>
    public int DiscoveredTopicsCount { get; set; }

    /// <summary>Artículos creados durante la corrida.</summary>
    public List<GeneratedArticleInfo> GeneratedArticles { get; set; } = [];

    /// <summary>Temáticas repetidas pendientes de decisión del usuario.</summary>
    public List<AutomationDecision> PendingDecisions { get; set; } = [];

    /// <summary>Hashes de títulos de temáticas procesadas (para deduplicación).</summary>
    public List<string> TopicHashes { get; set; } = [];

    /// <summary>Errores no fatales registrados durante la corrida.</summary>
    public List<string> Errors { get; set; } = [];

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Artículo creado por la automatización.</summary>
[BsonIgnoreExtraElements]
public class GeneratedArticleInfo
{
    public string ArticleId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;

    /// <summary>"Publicado" o "Borrador".</summary>
    public string Status { get; set; } = string.Empty;
}

/// <summary>Decisión editorial sobre una temática detectada como repetida.</summary>
[BsonIgnoreExtraElements]
public class AutomationDecision
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string TopicTitle { get; set; } = string.Empty;
    public string? Source { get; set; }
    public string TopicHash { get; set; } = string.Empty;
    public AutomationDecisionStatus Status { get; set; } = AutomationDecisionStatus.Pendiente;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
