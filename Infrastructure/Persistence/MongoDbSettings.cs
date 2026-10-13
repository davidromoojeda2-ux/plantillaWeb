namespace Infrastructure.Persistence;

/// <summary>
/// Configuración de conexión a MongoDB, enlazada desde appsettings.json bajo "MongoDB".
/// </summary>
public class MongoDbSettings
{
    public string ConnectionString { get; set; } = "mongodb://localhost:27017";
    public string DatabaseName { get; set; } = "portal_noticias";
}
