using System.Text.Json;
using Aplication.DTOs.ContentBlocks;
using Aplication.Serialization;
using Infrastructure.Security;

namespace ContenidoBloques.Tests;

public class ContentContractAndSecurityTests
{
    [Fact]
    public void Json_DeserializesSupportedDiscriminatorAndProperties()
    {
        const string json = """
        [
          { "type": "paragraph", "text": "<strong>Hola</strong>" },
          { "type": "heading", "level": 3, "text": "Título" },
          { "type": "table", "headers": ["A"], "rows": [["1"]] }
        ]
        """;

        var blocks = ContentBlockJson.Deserialize(json);

        Assert.Collection(
            blocks,
            b => Assert.Equal("<strong>Hola</strong>", Assert.IsType<ContentParagraphBlockDto>(b).Text),
            b => Assert.Equal(3, Assert.IsType<ContentHeadingBlockDto>(b).Level),
            b => Assert.Single(Assert.IsType<ContentTableBlockDto>(b).Rows));
    }

    [Fact]
    public void Json_RejectsUnknownType()
    {
        const string json = """[{ "type": "video", "url": "https://example.com" }]""";

        Assert.Throws<JsonException>(() => ContentBlockJson.Deserialize(json));
    }

    [Fact]
    public void Json_RejectsUnknownProperty()
    {
        const string json = """[{ "type": "paragraph", "text": "Hola", "danger": true }]""";

        Assert.Throws<JsonException>(() => ContentBlockJson.Deserialize(json));
    }

    [Fact]
    public void Json_RejectsMalformedAndOversizedPayloads()
    {
        Assert.Throws<JsonException>(() => ContentBlockJson.Deserialize("[{"));
        Assert.Throws<JsonException>(() => ContentBlockJson.Deserialize(null));
        Assert.Throws<JsonException>(() => ContentBlockJson.Deserialize(
            """[{ "type": "paragraph", "text": "Hola" }]""", maxBytes: 8));
    }

    [Fact]
    public void Json_SerializesDiscriminatorForTheEditor()
    {
        var json = ContentBlockJson.Serialize([new ContentParagraphBlockDto { Text = "Hola" }]);

        Assert.Contains("\"type\":\"paragraph\"", json);
        Assert.Contains("\"text\":\"Hola\"", json);
    }

    [Fact]
    public void Sanitizer_RemovesScriptsEventsStylesAndDangerousSchemes()
    {
        var sanitizer = new GanssHtmlSanitizer();

        var clean = sanitizer.Sanitize(
            "<p onclick=\"alert(1)\"><strong>Seguro</strong></p>" +
            "<script>alert('xss')</script>" +
            "<img src=\"x\" onerror=\"alert(1)\">" +
            "<a href=\"javascript:alert(1)\" style=\"color:red\">malo</a>" +
            "<iframe src=\"https://evil.example\"></iframe>");

        Assert.Contains("<strong>Seguro</strong>", clean);
        Assert.DoesNotContain("script", clean, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onerror", clean, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onclick", clean, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("javascript:", clean, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("style=", clean, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<iframe", clean, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Sanitizer_KeepsSafeLinksAndHardensExternalTargets()
    {
        var sanitizer = new GanssHtmlSanitizer();

        var clean = sanitizer.Sanitize("<a href=\"https://example.com\">ejemplo</a>");

        Assert.Contains("href=\"https://example.com\"", clean);
        Assert.Contains("target=\"_blank\"", clean);
        Assert.Contains("rel=\"noopener noreferrer\"", clean);
    }

    [Fact]
    public void Sanitizer_ToPlainTextDecodesAndNormalizesWhitespace()
    {
        var sanitizer = new GanssHtmlSanitizer();

        var text = sanitizer.ToPlainText("<strong>Hola</strong>&nbsp;  mundo<br>otra línea");

        Assert.Equal("Hola mundo otra línea", text);
    }
}
