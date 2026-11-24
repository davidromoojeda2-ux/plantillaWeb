using System.Runtime.CompilerServices;
using Infrastructure.Persistence;

namespace ContenidoBloques.Tests;

/// <summary>
/// Registra el mapeo BSON de producción una sola vez antes de ejecutar los tests.
/// Los tests de round-trip BSON ahora dependen de <see cref="MongoClassMaps"/>
/// (el dominio ya no lleva atributos de persistencia).
/// </summary>
internal static class TestBootstrap
{
    [ModuleInitializer]
    internal static void Initialize() => MongoClassMaps.Register();
}
