using Aplication.Configuration;
using Aplication.DTOs;
using Aplication.Mappers;
using Domine.Entities;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace ContenidoBloques.Tests;

public class DocumentDomainTests
{
    [Fact]
    public void Article_RoundTripsDocumentIdsThroughBson()
    {
        var documentId = ObjectId.GenerateNewId().ToString();
        var article = new Article
        {
            Title = "Artículo con documentos",
            Slug = "articulo-con-documentos",
            DocumentIds = [documentId]
        };

        var bson = article.ToBsonDocument();
        var restored = BsonSerializer.Deserialize<Article>(bson);

        Assert.Equal([documentId], restored.DocumentIds);
    }

    [Fact]
    public void DocumentMetadata_RoundTripsThroughBson()
    {
        var document = new DocumentMetadata
        {
            OriginalFileName = "informe.pdf",
            DisplayName = "Informe anual",
            Description = "Memoria del ejercicio",
            Extension = ".pdf",
            ContentType = "application/pdf",
            SizeBytes = 1024,
            Sha256 = "abc123",
            Storage = "local",
            Key = "documents/2026/11/informe.pdf",
            IsPublic = true,
            DownloadCount = 4
        };

        var bson = document.ToBsonDocument();
        var restored = BsonSerializer.Deserialize<DocumentMetadata>(bson);

        Assert.Equal(document.Id, restored.Id);
        Assert.Equal(document.DisplayName, restored.DisplayName);
        Assert.Equal(document.Storage, restored.Storage);
        Assert.Equal(document.Key, restored.Key);
        Assert.True(restored.IsPublic);
        Assert.Equal(4, restored.DownloadCount);
    }

    [Fact]
    public void DocumentMapper_RoundTripsFieldsUsedByTheUi()
    {
        var document = new DocumentMetadata
        {
            DisplayName = "Plan de trabajo",
            OriginalFileName = "plan.docx",
            Description = "Documento de planificación",
            Extension = ".docx",
            ContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            SizeBytes = 2048,
            Sha256 = "hash",
            Storage = "s3",
            Key = "documents/2026/11/plan.docx",
            IsPublic = false,
            DownloadCount = 2
        };

        var dto = DocumentMapper.ToDto(document);

        Assert.Equal(document.Id, dto.Id);
        Assert.Equal(document.DisplayName, dto.DisplayName);
        Assert.Equal(document.OriginalFileName, dto.OriginalFileName);
        Assert.Equal(document.SizeBytes, dto.SizeBytes);
        Assert.Equal(document.IsPublic, dto.IsPublic);
        Assert.Equal(document.DownloadCount, dto.DownloadCount);
    }

    [Fact]
    public void DocumentsOptions_BindsCustomValuesAndNormalizesExtensions()
    {
        var json = """
        {
          "Documents": {
            "AllowedExtensions": ["pdf", ".DOCX", ".PDF"],
            "MaxSizeBytes": 1048576,
            "MaxPerArticle": 3,
            "LocalPath": "custom/docs"
          }
        }
        """;

        var configuration = new ConfigurationBuilder()
            .AddJsonStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)))
            .Build();

        var options = configuration.GetSection(DocumentsOptions.SectionName).Get<DocumentsOptions>();

        Assert.NotNull(options);
        Assert.Equal(1048576, options!.MaxSizeBytes);
        Assert.Equal(3, options.MaxPerArticle);
        Assert.Equal("custom/docs", options.LocalPath);
        Assert.Contains(".pdf", options.NormalizedExtensions());
        Assert.Contains(".docx", options.NormalizedExtensions());
        Assert.Single(options.NormalizedExtensions().Where(extension =>
            extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public void DocumentsOptions_HasSafeDefaults()
    {
        var options = new DocumentsOptions();

        Assert.True(options.MaxSizeBytes > 0);
        Assert.True(options.MaxPerArticle > 0);
        Assert.Contains(".pdf", options.NormalizedExtensions());
        Assert.False(options.NormalizedExtensions().Contains(".html"));
    }

    [Fact]
    public void DocumentDto_ExposesDownloadMetadata()
    {
        var dto = new DocumentResponseDto
        {
            Id = "doc-1",
            DisplayName = "Informe",
            Extension = ".pdf",
            SizeBytes = 300,
            IsPublic = true
        };

        Assert.Equal("doc-1", dto.Id);
        Assert.Equal(".pdf", dto.Extension);
        Assert.True(dto.IsPublic);
    }
}
