using Aplication.Realtime;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace paginaWeb.Components;

/// <summary>
/// Base para islas que se suscriben al bus de cambios y refrescan su estado sin
/// polling. La suscripción se libera al salir del circuito o de la página.
/// </summary>
public abstract class RealtimeComponentBase : ComponentBase, IAsyncDisposable
{
    [Inject] protected ICmsChangeStream ChangeStream { get; set; } = default!;
    [Inject] protected IJSRuntime JS { get; set; } = default!;

    private CancellationTokenSource? _cts;
    private Task? _listenTask;
    private Task? _heartbeatTask;

    protected override void OnAfterRender(bool firstRender)
    {
        if (!firstRender) return;

        _cts = new CancellationTokenSource();
        _listenTask = ListenAsync(_cts.Token);
        _heartbeatTask = HeartbeatAsync(_cts.Token);
    }

    private async Task HeartbeatAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(20));

        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                try { await JS.InvokeVoidAsync("cmsLiveTouch"); }
                catch (JSDisconnectedException) { return; }
                catch (InvalidOperationException) { return; }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Navegación o apagado normal.
        }
    }

    private async Task ListenAsync(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var change in ChangeStream.SubscribeAsync(cancellationToken))
            {
                if (!Accepts(change)) continue;

                await InvokeAsync(async () =>
                {
                    await OnChangeAsync(change);
                    StateHasChanged();
                });
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Navegación o apagado normal.
        }
        catch (Exception ex)
        {
            await InvokeAsync(() =>
            {
                OnStreamError(ex);
                StateHasChanged();
            });
        }
    }

    protected virtual bool Accepts(CmsChangeEvent change) => true;

    protected abstract Task OnChangeAsync(CmsChangeEvent change);

    protected virtual void OnStreamError(Exception exception)
    {
    }

    public virtual async ValueTask DisposeAsync()
    {
        if (_cts is null) return;

        _cts.Cancel();

        if (_listenTask is not null)
        {
            try { await _listenTask; }
            catch (OperationCanceledException) { }
        }

        if (_heartbeatTask is not null)
        {
            try { await _heartbeatTask; }
            catch (OperationCanceledException) { }
        }

        _cts.Dispose();
        _cts = null;
    }
}
