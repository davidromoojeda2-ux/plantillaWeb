using Domine.Entities;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Infrastructure.Persistence;

/// <summary>
/// Contexto central de MongoDB. Expone colecciones tipadas para todas las entidades del dominio.
/// Registrar como Singleton en el contenedor DI.
/// </summary>
public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(IOptions<MongoDbSettings> settings)
    {
        var connStr = string.IsNullOrWhiteSpace(settings.Value.ConnectionString)
            ? "mongodb://localhost:27017"
            : settings.Value.ConnectionString;
        var dbName = string.IsNullOrWhiteSpace(settings.Value.DatabaseName)
            ? "portal_noticias"
            : settings.Value.DatabaseName;

        var client = new MongoClient(connStr);
        _database = client.GetDatabase(dbName);
    }

    public IMongoCollection<Category> Categories =>
        _database.GetCollection<Category>("categories");

    public IMongoCollection<Article> Articles =>
        _database.GetCollection<Article>("articles");

    public IMongoCollection<Comment> Comments =>
        _database.GetCollection<Comment>("comments");

    public IMongoCollection<Visitor> Visitors =>
        _database.GetCollection<Visitor>("visitors");

    public IMongoCollection<InformativePage> Pages =>
        _database.GetCollection<InformativePage>("pages");

    public IMongoCollection<ImageMetadata> Images =>
        _database.GetCollection<ImageMetadata>("images");

    public IMongoCollection<AdminUser> Users =>
        _database.GetCollection<AdminUser>("users");
}
