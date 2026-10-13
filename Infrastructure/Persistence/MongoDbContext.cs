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
        var client = new MongoClient(settings.Value.ConnectionString);
        _database = client.GetDatabase(settings.Value.DatabaseName);
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
