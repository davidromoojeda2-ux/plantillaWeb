namespace Infrastructure.Persistence;

/// <summary>
/// Configuración de conexión a MongoDB, enlazada desde appsettings.json bajo "MongoDB".
/// </summary>
public class MongoDbSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = "portal_noticias";
}
