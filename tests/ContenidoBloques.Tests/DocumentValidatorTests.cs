using System.Text;
using Aplication.Configuration;
using Aplication.Services;
using Domine.Exceptions;

namespace ContenidoBloques.Tests;

public class DocumentValidatorTests
{
    private static DocumentValidator Validator(long maxSizeBytes = 1024 * 1024) =>
        new(new DocumentsOptions { MaxSizeBytes = maxSizeBytes });

    private static MemoryStream StreamFrom(params byte[] bytes) => new(bytes);

    private static MemoryStream TextStream(string text) =>
        new(Encoding.UTF8.GetBytes(text));

    [Fact]
    public void Pdf_WithValidSignature_IsAccepted()
    {
        var validator = Validator();
        var content = TextStream("%PDF-1.7\n1 0 obj\n<<>>\nendobj\n");

        var result = validator.Validate("../../Informe Final.pdf", "Informe anual", content);

        Assert.Equal(".pdf", result.Extension);
        Assert.Equal("application/pdf", result.ContentType);
        Assert.Equal("Informe anual", result.DisplayName);
        Assert.Equal("informe-anual", result.StorageSlug);
        Assert.Equal(64, result.Sha256.Length);
    }

    [Fact]
    public void Pdf_WithHtmlContent_IsRejected()
    {
        var validator = Validator();
        var content = TextStream("<html><body>No soy un PDF</body></html>");

        var error = Assert.Throws<DomainValidationException>(() =>
            validator.Validate("informe.pdf", "Informe", content));

        Assert.Contains("no coincide", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DocxAndOleAndRtf_RecognizeTheirSignatures()
    {
        var validator = Validator();

        var docx = validator.Validate("plan.docx", null,
            StreamFrom(0x50, 0x4B, 0x03, 0x04, 0x14, 0x00));
        var ole = validator.Validate("legacy.doc", null,
            StreamFrom(0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1));
        var rtf = validator.Validate("nota.rtf", null,
            TextStream("{\\rtf1\\ansi Hola}"));

        Assert.Equal(".docx", docx.Extension);
        Assert.Equal(".doc", ole.Extension);
        Assert.Equal(".rtf", rtf.Extension);
        Assert.Equal("application/rtf", rtf.ContentType);
    }

    [Fact]
    public void Txt_WithPlainText_IsAccepted_ButHtmlDisguisedIsRejected()
    {
        var validator = Validator();

        var valid = validator.Validate("notas.txt", null, TextStream("Contenido plano"));
        Assert.Equal("text/plain; charset=utf-8", valid.ContentType);

        Assert.Throws<DomainValidationException>(() =>
            validator.Validate("notas.txt", null, TextStream("<script>alert(1)</script>")));
    }

    [Theory]
    [InlineData("pagina.html")]
    [InlineData("icono.svg")]
    [InlineData("script.js")]
    [InlineData("programa.exe")]
    [InlineData("archivo.pdf.exe")]
    public void ForbiddenExtensions_AreRejected(string fileName)
    {
        var validator = Validator();

        Assert.Throws<DomainValidationException>(() =>
            validator.Validate(fileName, null, TextStream("contenido")));
    }

    [Fact]
    public void FileLargerThanLimit_IsRejectedBeforeSignatureCheck()
    {
        var validator = Validator(maxSizeBytes: 8);
        var content = TextStream("%PDF-1.7 contenido demasiado grande");

        var error = Assert.Throws<DomainValidationException>(() =>
            validator.Validate("grande.pdf", null, content));

        Assert.Contains("tamaño máximo", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DisplayName_IsSanitizedAndKeepsPathOut()
    {
        var validator = Validator();

        var result = validator.Validate(
            "../../reporte final.pdf",
            "../../Nombre <peligroso>",
            TextStream("%PDF-1.7\n"));

        Assert.Equal("reporte final.pdf", result.OriginalFileName);
        Assert.DoesNotContain("..", result.DisplayName);
        Assert.DoesNotContain("/", result.DisplayName);
    }
}
