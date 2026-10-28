using Aplication.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ContenidoBloques.Tests;

public class ContentSecurityOptionsTests
{
    [Fact]
    public void Bind_ReadsConfiguredLimitsAndAllowedHosts()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ContentSecurity:MaxBlocks"] = "42",
                ["ContentSecurity:MaxJsonBytes"] = "1024",
                ["ContentSecurity:MaxTableColumns"] = "6",
                ["ContentSecurity:AllowedImageHosts:0"] = "images.example.com",
                ["ContentSecurity:AllowedImageHosts:1"] = "cdn.example.com"
            })
            .Build();

        var services = new ServiceCollection();
        services.Configure<ContentSecurityOptions>(
            configuration.GetSection(ContentSecurityOptions.SectionName));
        using var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<ContentSecurityOptions>>().Value;

        Assert.Equal(42, options.MaxBlocks);
        Assert.Equal(1024, options.MaxJsonBytes);
        Assert.Equal(6, options.MaxTableColumns);
        Assert.Equal(["images.example.com", "cdn.example.com"], options.AllowedImageHosts);
    }
}
