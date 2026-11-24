using System.Reflection;
using Aplication.Services;
using Domine.Entities;
using Infrastructure.Persistence;

namespace ContenidoBloques.Tests;

/// <summary>
/// Guarda de arquitectura: garantiza que las capas internas no vuelvan a acoplarse
/// a MongoDB. Si alguien reintroduce una dependencia del driver en <c>Domine</c> o
/// <c>Aplication</c>, estos tests fallan.
/// </summary>
public class ArchitectureDecouplingTests
{
    [Fact]
    public void Domine_NoReferenceMongoDB()
    {
        var references = typeof(Article).Assembly.GetReferencedAssemblies();
        Assert.DoesNotContain(references, IsMongoAssembly);
    }

    [Fact]
    public void Aplication_NoReferenceMongoDB()
    {
        var references = typeof(ArticleService).Assembly.GetReferencedAssemblies();
        Assert.DoesNotContain(references, IsMongoAssembly);
    }

    [Fact]
    public void Infrastructure_DoesReferenceMongoDB()
    {
        var references = typeof(MongoDbContext).Assembly.GetReferencedAssemblies();
        Assert.Contains(references, IsMongoAssembly);
    }

    private static bool IsMongoAssembly(AssemblyName name) =>
        name.Name?.StartsWith("MongoDB", StringComparison.Ordinal) == true;
}
