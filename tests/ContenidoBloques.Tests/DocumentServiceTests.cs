using System.Text;
using Aplication.Configuration;
using Aplication.DTOs;
using Aplication.Interfaces;
using Aplication.Services;
using Domine.Entities;
using Domine.Exceptions;
using Infrastructure.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace ContenidoBloques.Tests;

public class DocumentServiceTests
{
    private readonly DocumentsOptions _options = new()
    {
        MaxSizeBytes = 1024 * 1024,
        MaxPerArticle = 10
    };

    [Fact]
    public async Task Upload_ValidPdf_StoresBinaryAndMetadata()
    {
        var storage = LocalStorage();
        var repo = Repository();
        DocumentMetadata? inserted = null;
        repo.Setup(r => r.InsertAsync(It.IsAny<DocumentMetadata>()))
            .Callback<DocumentMetadata>(document => inserted = document)
            .Returns(Task.CompletedTask);

        var service = BuildService(storage, repo, Articles());

        var result = await service.UploadAsync(new DocumentUploadRequest
        {
            Content = PdfStream(),
            OriginalFileName = "informe.pdf",
            DisplayName = "Informe anual",
            Description = "Memoria del ejercicio",
            IsPublic = true
        });

        Assert.NotNull(inserted);
        Assert.Equal("Informe anual", inserted!.DisplayName);
        Assert.Equal(".pdf", inserted.Extension);
        Assert.Equal("application/pdf", inserted.ContentType);
        Assert.Equal("local", inserted.Storage);
        Assert.True(inserted.IsPublic);
        Assert.StartsWith("documents/", inserted.Key);
        Assert.Equal(inserted.Id, result.Id);
        storage.Verify(s => s.UploadAsync(
            It.Is<string>(key => key.StartsWith("documents/")),
            It.IsAny<Stream>(),
            "application/pdf",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Upload_WhenCloudFails_FallsBackToLocalStorage()
    {
        var cloud = new Mock<IDocumentStorage>();
        cloud.SetupGet(s => s.StorageType).Returns("s3");
        cloud.Setup(s => s.IsAvailableAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        cloud.Setup(s => s.UploadAsync(
                It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("S3 caído"));

        var local = LocalStorage();
        var repo = Repository();
        DocumentMetadata? inserted = null;
        repo.Setup(r => r.InsertAsync(It.IsAny<DocumentMetadata>()))
            .Callback<DocumentMetadata>(document => inserted = document)
            .Returns(Task.CompletedTask);

        var service = BuildService(new[] { cloud.Object, local.Object }, repo, Articles());

        await service.UploadAsync(new DocumentUploadRequest
        {
            Content = PdfStream(),
            OriginalFileName = "informe.pdf"
        });

        Assert.Equal("local", inserted!.Storage);
        local.Verify(s => s.UploadAsync(
            It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Upload_WhenPersistenceFails_CompensatesStoredObject()
    {
        var storage = LocalStorage();
        var repo = Repository();
        repo.Setup(r => r.InsertAsync(It.IsAny<DocumentMetadata>()))
            .ThrowsAsync(new InvalidOperationException("Mongo caído"));

        var service = BuildService(storage, repo, Articles());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UploadAsync(new DocumentUploadRequest
            {
                Content = PdfStream(),
                OriginalFileName = "informe.pdf"
            }));

        storage.Verify(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Upload_FileOverLimit_IsRejectedWithoutStoring()
    {
        var storage = LocalStorage();
        var repo = Repository();
        var service = BuildService(storage, repo, Articles());

        await Assert.ThrowsAsync<DomainValidationException>(() =>
            service.UploadAsync(new DocumentUploadRequest
            {
                Content = new MemoryStream(Encoding.UTF8.GetBytes(new string('x', (int)_options.MaxSizeBytes + 1))),
                OriginalFileName = "grande.txt"
            }));

        storage.Verify(s => s.UploadAsync(
            It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetPaged_PassesFiltersToRepository()
    {
        var repo = Repository();
        repo.Setup(r => r.CountAsync("informe", "local", ".pdf", "linked", It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);
        repo.Setup(r => r.GetPagedAsync(10, 10, "informe", "local", ".pdf", "linked", It.IsAny<CancellationToken>()))
            .ReturnsAsync([new DocumentMetadata { DisplayName = "A" }, new DocumentMetadata { DisplayName = "B" }]);

        var service = BuildService(LocalStorage(), repo, Articles());

        var (items, total) = await service.GetPagedAsync(2, 10, "informe", "local", ".pdf", "linked");

        Assert.Equal(2, total);
        Assert.Equal(2, items.Count);
    }

    [Fact]
    public async Task UpdateMetadata_ChangesOnlyVisibleFields()
    {
        var document = new DocumentMetadata
        {
            DisplayName = "Viejo",
            OriginalFileName = "informe.pdf",
            Extension = ".pdf",
            Key = "documents/2026/11/informe.pdf",
            Storage = "local"
        };

        var repo = Repository();
        repo.Setup(r => r.GetByIdAsync(document.Id)).ReturnsAsync(document);

        var service = BuildService(LocalStorage(), repo, Articles());
        var result = await service.UpdateMetadataAsync(document.Id, "  Nuevo nombre  ", " Descripción ", true);

        Assert.Equal("Nuevo nombre", result.DisplayName);
        Assert.Equal("Descripción", result.Description);
        Assert.True(result.IsPublic);
        repo.Verify(r => r.UpdateMetadataAsync(document.Id, "Nuevo nombre", "Descripción", true), Times.Once);
    }

    [Fact]
    public async Task Delete_RemovesObjectAndUnlinksArticles()
    {
        var document = new DocumentMetadata
        {
            DisplayName = "Informe",
            OriginalFileName = "informe.pdf",
            Extension = ".pdf",
            Key = "documents/2026/11/informe.pdf",
            Storage = "local"
        };
        var article = new Article { DocumentIds = [document.Id] };

        var storage = LocalStorage();
        var repo = Repository();
        repo.Setup(r => r.GetByIdAsync(document.Id)).ReturnsAsync(document);
        var articles = Articles();
        articles.Setup(r => r.GetAllAsync(0, 5000)).ReturnsAsync([article]);

        var service = BuildService(storage, repo, articles);

        await service.DeleteAsync(document.Id);

        storage.Verify(s => s.DeleteAsync(document.Key, It.IsAny<CancellationToken>()), Times.Once);
        articles.Verify(r => r.UpdateAsync(It.Is<Article>(a => a.DocumentIds.Count == 0)), Times.Once);
        repo.Verify(r => r.DeleteAsync(document.Id), Times.Once);
    }

    [Fact]
    public async Task CanDownloadPublic_AllowsPublicAndPublishedArticleDocuments()
    {
        var publicDocument = new DocumentMetadata { Id = "public", IsPublic = true };
        var linkedDocument = new DocumentMetadata { Id = "linked", IsPublic = false };
        var draftDocument = new DocumentMetadata { Id = "draft", IsPublic = false };

        var repo = Repository();
        repo.Setup(r => r.GetByIdAsync("public")).ReturnsAsync(publicDocument);
        repo.Setup(r => r.GetByIdAsync("linked")).ReturnsAsync(linkedDocument);
        repo.Setup(r => r.GetByIdAsync("draft")).ReturnsAsync(draftDocument);
        repo.Setup(r => r.GetByIdAsync("missing")).ReturnsAsync((DocumentMetadata?)null);

        var articles = Articles();
        articles.Setup(r => r.IsDocumentReferencedByPublishedArticleAsync("linked", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        articles.Setup(r => r.IsDocumentReferencedByPublishedArticleAsync("draft", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var service = BuildService(LocalStorage(), repo, articles);

        Assert.True(await service.CanDownloadPublicAsync("public"));
        Assert.True(await service.CanDownloadPublicAsync("linked"));
        Assert.False(await service.CanDownloadPublicAsync("draft"));
        Assert.False(await service.CanDownloadPublicAsync("missing"));
    }

    [Fact]
    public async Task OpenDownload_UsesRangeAndReturnsSafeFileName()
    {
        var document = new DocumentMetadata
        {
            DisplayName = "Informe anual",
            OriginalFileName = "informe.pdf",
            Extension = ".pdf",
            ContentType = "application/pdf",
            Key = "documents/2026/11/informe.pdf",
            Storage = "local",
            SizeBytes = 100,
            Sha256 = "hash",
            IsPublic = true
        };

        var storage = LocalStorage();
        storage.Setup(s => s.OpenReadAsync(document.Key, 10, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream([1, 2, 3]));

        var repo = Repository();
        repo.Setup(r => r.GetByIdAsync(document.Id)).ReturnsAsync(document);

        var service = BuildService(storage, repo, Articles());
        var result = await service.OpenDownloadAsync(document.Id, requirePublic: true, offset: 10, length: 20);

        Assert.NotNull(result);
        Assert.Equal("Informe anual.pdf", result!.FileName);
        Assert.Equal("application/pdf", result.ContentType);
        storage.Verify(s => s.OpenReadAsync(document.Key, 10, 20, It.IsAny<CancellationToken>()), Times.Once);
    }

    private DocumentService BuildService(
        Mock<IDocumentStorage> storage,
        Mock<IDocumentRepository> repo,
        Mock<IArticleRepository> articles) =>
        BuildService([storage.Object], repo, articles);

    private DocumentService BuildService(
        IEnumerable<IDocumentStorage> storages,
        Mock<IDocumentRepository> repo,
        Mock<IArticleRepository> articles) =>
        new(
            storages,
            repo.Object,
            articles.Object,
            new DocumentValidator(_options),
            Options.Create(_options),
            NullLogger<DocumentService>.Instance);

    private static Mock<IDocumentStorage> LocalStorage()
    {
        var storage = new Mock<IDocumentStorage>();
        storage.SetupGet(s => s.StorageType).Returns("local");
        storage.Setup(s => s.IsAvailableAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        storage.Setup(s => s.UploadAsync(
                It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        storage.Setup(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return storage;
    }

    private static Mock<IDocumentRepository> Repository()
    {
        var repo = new Mock<IDocumentRepository>();
        repo.Setup(r => r.InsertAsync(It.IsAny<DocumentMetadata>())).Returns(Task.CompletedTask);
        repo.Setup(r => r.UpdateMetadataAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<bool>()))
            .Returns(Task.CompletedTask);
        repo.Setup(r => r.IncrementDownloadCountAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
        repo.Setup(r => r.DeleteAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
        return repo;
    }

    private static Mock<IArticleRepository> Articles()
    {
        var articles = new Mock<IArticleRepository>();
        articles.Setup(r => r.GetAllAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync([]);
        articles.Setup(r => r.UpdateAsync(It.IsAny<Article>())).Returns(Task.CompletedTask);
        articles.Setup(r => r.IsDocumentReferencedByPublishedArticleAsync(
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        return articles;
    }

    private static MemoryStream PdfStream() =>
        new(Encoding.UTF8.GetBytes("%PDF-1.7\n1 0 obj\n<<>>\nendobj\n"));
}
