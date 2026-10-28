using Domine.ContentBlocks;
using Infrastructure.Persistence;

namespace ContenidoBloques.Tests;

public class SeedContentBlocksTests
{
    [Fact]
    public void FromHtml_ConvertsAllSeedBlockShapes()
    {
        const string html = """
        <h2>Encabezado</h2>
        <p>Párrafo con <strong>negrita</strong>.</p>
        <ol><li>Uno</li><li>Dos</li></ol>
        <ul><li>Viñeta</li></ul>
        <div class="callout callout-warning">
          <div class="callout-body"><strong>Atención:</strong> texto del callout.</div>
        </div>
        <div class="table-responsive-wrapper">
          <table>
            <thead><tr><th>Columna A</th><th>Columna B</th></tr></thead>
            <tbody><tr><td>1</td><td>2</td></tr></tbody>
          </table>
        </div>
        <figure>
          <img src="https://images.unsplash.com/photo-1?w=800" alt="Foto" width="800" height="600">
          <figcaption>Pie de foto</figcaption>
        </figure>
        <blockquote><p>Cita textual</p><cite>Autora</cite></blockquote>
        <pre><code class="language-csharp">var x = 1;</code></pre>
        <hr>
        """;

        var blocks = SeedContentBlocks.FromHtml(html);

        Assert.Collection(
            blocks,
            b => Assert.Equal("Encabezado", Assert.IsType<ContentHeadingBlock>(b).Text.Value),
            b => Assert.IsType<ContentParagraphBlock>(b),
            b => Assert.Equal(2, Assert.IsType<ContentOrderedListBlock>(b).Items.Count),
            b => Assert.Single(Assert.IsType<ContentBulletListBlock>(b).Items),
            b => Assert.Equal(CalloutVariant.Warning, Assert.IsType<ContentCalloutBlock>(b).Variant),
            b =>
            {
                var table = Assert.IsType<ContentTableBlock>(b);
                Assert.Equal(2, table.Headers.Count);
                Assert.Single(table.Rows);
            },
            b =>
            {
                var image = Assert.IsType<ContentImageBlock>(b);
                Assert.Equal("Foto", image.Alt.Value);
                Assert.Equal("Pie de foto", image.Caption!.Value);
                Assert.Equal(800, image.Width);
            },
            b =>
            {
                var quote = Assert.IsType<ContentQuoteBlock>(b);
                Assert.Equal("Cita textual", quote.Text.Text);
                Assert.Equal("Autora", quote.Attribution!.Value);
            },
            b =>
            {
                var code = Assert.IsType<ContentCodeBlock>(b);
                Assert.Equal("var x = 1;", code.Code.Value);
                Assert.Equal("csharp", code.Language!.Value);
            },
            b => Assert.IsType<ContentDividerBlock>(b));
    }
}
