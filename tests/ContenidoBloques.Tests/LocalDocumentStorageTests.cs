using System.Text;
using Aplication.Configuration;
using Infrastructure.Storage;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace ContenidoBloques.Tests;

public class LocalDocumentStorageTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(),
        "documentos-cms-tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task UploadAndOpenRange_ReturnsOnlyRequestedBytes()
    {
        var storage = BuildStorage();
        var content = Encoding.UTF8.GetBytes("0123456789");

        await storage.UploadAsync("documents/prueba.txt", new MemoryStream(content), "text/plain");

        await using var range = await storage.OpenReadAsync("documents/prueba.txt", offset: 2, length: 4);

        Assert.NotNull(range);
        using var reader = new StreamReader(range!);
        Assert.Equal("2345", await reader.ReadToEndAsync());
    }

    [Fact]
    public async Task OpenRead_ReturnsNullForMissingFile()
    {
        var storage = BuildStorage();

        var stream = await storage.OpenReadAsync("documents/no-existe.pdf");

        Assert.Null(stream);
    }

    [Fact]
    public async Task Upload_RejectsKeyThatEscapesTheRoot()
    {
        var storage = BuildStorage();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            storage.UploadAsync("../fuera.txt", new MemoryStream([1, 2, 3]), "text/plain"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    private LocalDocumentStorage BuildStorage()
    {
        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(e => e.ContentRootPath).Returns(_root);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Documents:LocalPath"] = "storage/documents"
            })
            .Build();

        return new LocalDocumentStorage(
            environment.Object,
            configuration,
            Options.Create(new DocumentsOptions()),
            NullLogger<LocalDocumentStorage>.Instance);
    }
}
