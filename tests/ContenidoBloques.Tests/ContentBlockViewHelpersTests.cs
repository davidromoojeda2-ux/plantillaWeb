using paginaWeb.Helpers;

namespace ContenidoBloques.Tests;

public class ContentBlockViewHelpersTests
{
    [Fact]
    public void Destaca_la_etiqueta_inicial_de_un_elemento_de_lista()
    {
        var html = ContentBlockViewHelpers.HighlightListLead(
            "Solar de Naturgy: ventaja principal y batería sin caducidad.");

        Assert.Equal(
            "<span class=\"list-item-lead\">Solar de Naturgy:</span> ventaja principal y batería sin caducidad.",
            html);
    }

    [Fact]
    public void No_duplica_el_destacado_si_el_item_ya_empieza_marcado()
    {
        const string html = "<strong>Solar de Naturgy:</strong> ventaja principal.";

        Assert.Equal(html, ContentBlockViewHelpers.HighlightListLead(html));
    }

    [Fact]
    public void No_destaca_una_url_como_etiqueta()
    {
        const string html = "https://example.com: ruta de ejemplo.";

        Assert.Equal(html, ContentBlockViewHelpers.HighlightListLead(html));
    }

    [Fact]
    public void No_destaca_etiquetas_demasiado_largas()
    {
        var html = new string('a', 81) + ": descripción.";

        Assert.Equal(html, ContentBlockViewHelpers.HighlightListLead(html));
    }

    [Fact]
    public void No_destaca_un_prefijo_sin_letras()
    {
        const string html = "10:30: reunión del equipo.";

        Assert.Equal(html, ContentBlockViewHelpers.HighlightListLead(html));
    }
}
