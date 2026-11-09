using Aplication.Interfaces;
using Aplication.Services;
using Domine.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using paginaWeb.Workers;

namespace ArticleAutomation.Tests;

public class ArticleAutomationWorkerTests
{
    [Fact]
    public async Task Worker_Survives_Exception_And_Continues()
    {
        var calls = 0;
        var fake = new Mock<IAutomationService>();
        fake.Setup(s => s.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AutomationSettings { Enabled = true, Paused = false, FrequencyMinutes = 1 });
        fake.Setup(s => s.RunOnceAsync(It.IsAny<AutomationTrigger>(), It.IsAny<CancellationToken>()))
            .Callback(() => Interlocked.Increment(ref calls))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var coordinator = new AutomationCoordinator();

        var services = new ServiceCollection();
        services.AddScoped<IAutomationService>(_ => fake.Object);
        var provider = services.BuildServiceProvider();

        var worker = new ArticleAutomationWorker(
            provider.GetRequiredService<IServiceScopeFactory>(),
            coordinator,
            NullLogger<ArticleAutomationWorker>.Instance);

        await worker.StartAsync(CancellationToken.None);

        for (var i = 0; i < 2; i++)
        {
            coordinator.SignalRun();
            await WaitUntilAsync(() => Volatile.Read(ref calls) >= i + 1, TimeSpan.FromSeconds(5));
        }

        await worker.StopAsync(CancellationToken.None);

        Assert.True(Volatile.Read(ref calls) >= 2);
    }

    private static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow.Add(timeout);
        while (!condition() && DateTime.UtcNow < deadline)
            await Task.Delay(20);

        Assert.True(condition(), "La condición no se cumplió en el tiempo esperado.");
    }
}
