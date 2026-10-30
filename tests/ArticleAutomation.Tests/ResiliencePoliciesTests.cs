using Infrastructure.Resilience;

namespace ArticleAutomation.Tests;

public class ResiliencePoliciesTests
{
    [Fact]
    public async Task Pipeline_Retries_On_First_Failure()
    {
        var pipeline = ResiliencePolicies.CreateHttpPipeline();
        var calls = 0;

        async ValueTask<int> Flaky(CancellationToken ct)
        {
            var n = Interlocked.Increment(ref calls);
            if (n == 1) throw new HttpRequestException("fallo transitorio");
            return 42;
        }

        var result = await pipeline.ExecuteAsync(Flaky, CancellationToken.None);

        Assert.Equal(42, result);
        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task Pipeline_Opens_Circuit_After_Repeated_Failures()
    {
        var pipeline = ResiliencePolicies.CreateHttpPipeline();
        var calls = 0;

        async ValueTask<int> AlwaysFail(CancellationToken ct)
        {
            Interlocked.Increment(ref calls);
            await Task.Yield();
            throw new HttpRequestException("boom");
        }

        for (var i = 0; i < 6; i++)
        {
            await Assert.ThrowsAnyAsync<Exception>(async () =>
            {
                await pipeline.ExecuteAsync(AlwaysFail, CancellationToken.None);
            });
        }

        var before = calls;
        await Assert.ThrowsAnyAsync<Exception>(async () =>
        {
            await pipeline.ExecuteAsync(AlwaysFail, CancellationToken.None);
        });

        // Una vez abierto el circuito, el siguiente intento falla rápido sin invocar la operación.
        Assert.Equal(before, calls);
    }
}
