namespace Aplication.Realtime;

/// <summary>Módulos del panel administrativo que exponen cambios en vivo.</summary>
public enum CmsModule
{
    Automation,
    Articles,
    Categories,
    Comments
}

/// <summary>Operación que originó un cambio visible en el panel.</summary>
public enum CmsChangeOperation
{
    Created,
    Updated,
    Deleted,
    Published,
    ArticleGenerated,
    Started,
    Completed,
    Failed,
    Stopped,
    DecisionCreated,
    DecisionResolved,
    SettingsChanged
}

/// <summary>
/// Evento mínimo de invalidación. No transporta entidades completas ni secretos:
/// los consumidores usan la versión y el identificador para decidir qué refrescar.
/// </summary>
public sealed record CmsChangeEvent(
    CmsModule Module,
    string? EntityId,
    CmsChangeOperation Operation,
    long Version,
    DateTime OccurredAt);

/// <summary>Publica cambios ya confirmados y mantiene la versión de cada módulo.</summary>
public interface ICmsChangePublisher
{
    long Publish(CmsModule module, string? entityId, CmsChangeOperation operation);
    long GetVersion(CmsModule module);
    IReadOnlyDictionary<CmsModule, long> GetVersions();
}

/// <summary>Flujo de cambios para componentes interactivos y polling de respaldo.</summary>
public interface ICmsChangeStream
{
    IAsyncEnumerable<CmsChangeEvent> SubscribeAsync(CancellationToken cancellationToken = default);
}
