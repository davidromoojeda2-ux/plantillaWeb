using Aplication.Services;

namespace ArticleAutomation.Tests;

public class AutomationCoordinatorTests
{
    [Fact]
    public async Task Signal_Run_Wakes_Wait()
    {
        var coordinator = new AutomationCoordinator();
        coordinator.SignalRun();

        var got = await coordinator.WaitForSignalAsync(TimeSpan.FromSeconds(5), CancellationToken.None);

        Assert.True(got);
    }

    [Fact]
    public void Pause_Flag_Toggles()
    {
        var coordinator = new AutomationCoordinator();

        coordinator.SetPaused(true);
        Assert.True(coordinator.IsPaused);

        coordinator.SetPaused(false);
        Assert.False(coordinator.IsPaused);
    }

    [Fact]
    public async Task Run_Lock_Prevents_Concurrency()
    {
        var coordinator = new AutomationCoordinator();

        Assert.True(await coordinator.TryBeginRunAsync(CancellationToken.None));
        Assert.False(await coordinator.TryBeginRunAsync(CancellationToken.None));

        coordinator.EndRun();
        Assert.True(await coordinator.TryBeginRunAsync(CancellationToken.None));
        coordinator.EndRun();
    }
}
