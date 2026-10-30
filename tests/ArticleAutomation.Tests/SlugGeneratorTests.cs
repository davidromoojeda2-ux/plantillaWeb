using Aplication.Services;

namespace ArticleAutomation.Tests;

public class SlugGeneratorTests
{
    [Fact]
    public void Hash_Is_Insensitive_To_Accents_And_Case()
    {
        var a = SlugGenerator.Hash("Tecnología y Sociedad");
        var b = SlugGenerator.Hash("tecnologia y sociedad");

        Assert.Equal(a, b);
    }

    [Fact]
    public void Slugify_Removes_Accents_And_Normalizes()
    {
        Assert.Equal("tecnologia-sociedad", SlugGenerator.Slugify("¡Tecnología & Sociedad!"));
    }
}
