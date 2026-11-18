using System.Text;
using Aplication.DTOs;
using Microsoft.AspNetCore.Http;
using paginaWeb.Helpers;

namespace ContenidoBloques.Tests;

public class DocumentDownloadWriterTests
{
    [Fact]
    public void ParseRange_ReturnsFullLengthWhenHeaderIsMissing()
    {
        Assert.True(DocumentDownloadWriter.TryParseRange(null, 100, out var offset, out var length, out var partial));

        Assert.Equal(0, offset);
        Assert.Equal(100, length);
        Assert.False(partial);
    }

    [Theory]
    [InlineData("bytes=10-19", 10, 10)]
    [InlineData("bytes=90-", 90, 10)]
    [InlineData("bytes=-5", 95, 5)]
    [InlineData("bytes=0-500", 0, 100)]
    public void ParseRange_ReturnsRequestedWindow(string header, long expectedOffset, long expectedLength)
    {
        Assert.True(DocumentDownloadWriter.TryParseRange(header, 100, out var offset, out var length, out var partial));

        Assert.True(partial);
        Assert.Equal(expectedOffset, offset);
        Assert.Equal(expectedLength, length);
    }

    [Theory]
    [InlineData("bytes=100-200")]
    [InlineData("bytes=20-10")]
    [InlineData("items=0-1")]
    [InlineData("bytes=0-1,3-4")]
    public void ParseRange_RejectsInvalidOrMultipleRanges(string header)
    {
        Assert.False(DocumentDownloadWriter.TryParseRange(header, 100, out _, out _, out _));
    }

    [Fact]
    public async Task WriteAsync_FullDownload_SetsSafeHeadersAndBody()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var payload = Encoding.UTF8.GetBytes("0123456789");
        var document = Result(payload);

        await DocumentDownloadWriter.WriteAsync(context.Response, document, null, null, publicCache: true, CancellationToken.None);

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.Equal("application/pdf", context.Response.ContentType);
        Assert.Equal("nosniff", context.Response.Headers.XContentTypeOptions.ToString());
        Assert.Equal("bytes", context.Response.Headers.AcceptRanges.ToString());
        Assert.Contains("attachment", context.Response.Headers.ContentDisposition.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Informe anual.pdf", context.Response.Headers.ContentDisposition.ToString());

        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        Assert.Equal("0123456789", await reader.ReadToEndAsync());
    }

    [Fact]
    public async Task WriteAsync_PartialDownload_Sets206AndContentRange()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var document = Result(Encoding.UTF8.GetBytes("0123456789"));

        await DocumentDownloadWriter.WriteAsync(context.Response, document, 2, 4, publicCache: false, CancellationToken.None);

        Assert.Equal(StatusCodes.Status206PartialContent, context.Response.StatusCode);
        Assert.Equal("bytes 2-5/10", context.Response.Headers.ContentRange.ToString());
        Assert.Equal("private, no-store", context.Response.Headers.CacheControl.ToString());
        Assert.Equal(4, context.Response.ContentLength);
    }

    private static DocumentDownloadResult Result(byte[] payload) => new(
        new MemoryStream(payload),
        "Informe anual.pdf",
        "application/pdf",
        payload.Length,
        "hash-123",
        DateTime.UtcNow,
        true);
}
