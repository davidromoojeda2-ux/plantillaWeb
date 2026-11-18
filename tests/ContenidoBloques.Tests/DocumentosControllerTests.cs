using System.Text;
using Aplication.Configuration;
using Aplication.DTOs;
using Aplication.Interfaces;
using Aplication.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using paginaWeb.Areas.Admin.Controllers;

namespace ContenidoBloques.Tests;

public class DocumentosControllerTests
{
    [Fact]
    public async Task UploadAjax_WithEmptyFile_ReturnsBadRequest()
    {
        var controller = BuildController();
        var file = new Mock<IFormFile>();
        file.SetupGet(f => f.Length).Returns(0);

        var result = await controller.UploadAjax(file.Object, null, null, false);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
    }

    [Fact]
    public async Task UploadAjax_WithValidFile_ReturnsDocumentJson()
    {
        var documentService = new Mock<IDocumentService>();
        documentService.Setup(service => service.UploadAsync(
                It.IsAny<DocumentUploadRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DocumentResponseDto
            {
                Id = "doc-1",
                DisplayName = "Informe",
                OriginalFileName = "informe.pdf",
                Extension = ".pdf",
                ContentType = "application/pdf",
                SizeBytes = 100,
                Storage = "local",
                IsPublic = false
            });

        var controller = BuildController(documentService);
        var file = new Mock<IFormFile>();
        file.SetupGet(f => f.Length).Returns(100);
        file.SetupGet(f => f.FileName).Returns("informe.pdf");
        file.Setup(f => f.OpenReadStream()).Returns(() => new MemoryStream(Encoding.UTF8.GetBytes("%PDF-1.7")));

        var result = await controller.UploadAjax(file.Object, "Informe", "Descripción", false);

        var json = Assert.IsType<JsonResult>(result);
        var serialized = System.Text.Json.JsonSerializer.Serialize(json.Value);
        Assert.Contains("\"success\":true", serialized);
        Assert.Contains("doc-1", serialized);
        documentService.Verify(service => service.UploadAsync(
            It.Is<DocumentUploadRequest>(request => request.OriginalFileName == "informe.pdf"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Rename_WhenServiceRejects_ReturnsBadRequest()
    {
        var documentService = new Mock<IDocumentService>();
        documentService.Setup(service => service.UpdateMetadataAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Domine.Exceptions.DomainValidationException("El nombre visible es obligatorio."));

        var controller = BuildController(documentService);

        var result = await controller.Rename("doc-1", "", null, false);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    private static DocumentosController BuildController(Mock<IDocumentService>? documentService = null)
    {
        documentService ??= new Mock<IDocumentService>();
        var articleService = new Mock<IArticleService>();

        return new DocumentosController(
            documentService.Object,
            articleService.Object,
            Options.Create(new DocumentsOptions()));
    }
}
