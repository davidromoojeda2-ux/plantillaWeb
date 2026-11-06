using Aplication.Realtime;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using paginaWeb.Realtime;

namespace ArticleAutomation.Tests;

public class CmsChangeBusTests
{
    private static CmsChangeBus CreateBus(int windowMs = 10, int capacity = 16) =>
        new(
            NullLogger<CmsChangeBus>.Instance,
            Options.Create(new CmsRealtimeOptions
            {
                CoalesceWindowMilliseconds = windowMs,
                SubscriberQueueCapacity = capacity
            }));

    [Fact]
    public void Publish_IncrementsVersionsPerModule()
    {
        var bus = CreateBus();

        Assert.Equal(1, bus.Publish(CmsModule.Articles, "a1", CmsChangeOperation.Created));
        Assert.Equal(2, bus.Publish(CmsModule.Articles, "a2", CmsChangeOperation.Updated));
        Assert.Equal(1, bus.Publish(CmsModule.Comments, "c1", CmsChangeOperation.Created));

        Assert.Equal(2, bus.GetVersion(CmsModule.Articles));
        Assert.Equal(1, bus.GetVersion(CmsModule.Comments));
        Assert.Equal(0, bus.GetVersion(CmsModule.Categories));
    }

    [Fact]
    public async Task Subscribe_ReceivesCoalescedChange()
    {
        await using var bus = CreateBus();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        var received = ReadOneAsync(bus, cts.Token);
        await Task.Delay(50, cts.Token);

        bus.Publish(CmsModule.Articles, "a1", CmsChangeOperation.Created);
        bus.Publish(CmsModule.Articles, "a1", CmsChangeOperation.Updated);

        var change = await received;

        Assert.NotNull(change);
        Assert.Equal(CmsModule.Articles, change!.Module);
        Assert.Equal(2, change.Version);
        Assert.Equal("a1", change.EntityId);
    }

    [Fact]
    public async Task Subscribe_BroadcastsToEverySubscriber()
    {
        await using var bus = CreateBus();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        var first = ReadOneAsync(bus, cts.Token);
        var second = ReadOneAsync(bus, cts.Token);
        await Task.Delay(50, cts.Token);

        bus.Publish(CmsModule.Comments, null, CmsChangeOperation.Updated);

        var a = await first;
        var b = await second;

        Assert.NotNull(a);
        Assert.NotNull(b);
        Assert.Equal(a!.Version, b!.Version);
        Assert.Equal(CmsModule.Comments, b.Module);
    }

    [Fact]
    public async Task Subscribe_UnsubscribesWhenCancelled()
    {
        await using var bus = CreateBus();
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        var received = ReadOneAsync(bus, cts.Token);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => received);
    }

    private static async Task<CmsChangeEvent?> ReadOneAsync(CmsChangeBus bus, CancellationToken cancellationToken)
    {
        await foreach (var change in bus.SubscribeAsync(cancellationToken))
            return change;

        return null;
    }
}
