using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Aplication.Realtime;
using Microsoft.Extensions.Options;

namespace paginaWeb.Realtime;

/// <summary>Opciones del bus de cambios del panel administrativo.</summary>
public sealed class CmsRealtimeOptions
{
    /// <summary>Ventana de agrupación para evitar ráfagas de renders.</summary>
    public int CoalesceWindowMilliseconds { get; set; } = 250;

    /// <summary>Capacidad de la cola de cada suscriptor.</summary>
    public int SubscriberQueueCapacity { get; set; } = 256;
}

/// <summary>
/// Bus en proceso con versiones por módulo, coalescencia y una cola acotada por
/// suscriptor. Un cliente lento no bloquea a los demás ni a las escrituras.
/// </summary>
public sealed class CmsChangeBus : ICmsChangePublisher, ICmsChangeStream, IAsyncDisposable
{
    private readonly long[] _versions = new long[Enum.GetValues<CmsModule>().Length];
    private readonly ConcurrentDictionary<string, CmsChangeEvent> _pending = new();
    private readonly ConcurrentDictionary<Guid, Channel<CmsChangeEvent>> _subscribers = new();
    private readonly SemaphoreSlim _flushSignal = new(0, 1);
    private readonly CancellationTokenSource _cts = new();
    private readonly TimeSpan _coalesceWindow;
    private readonly int _queueCapacity;
    private readonly ILogger<CmsChangeBus> _logger;
    private readonly Task _dispatchLoop;

    public CmsChangeBus(ILogger<CmsChangeBus> logger, IOptions<CmsRealtimeOptions> options)
    {
        _logger = logger;
        _coalesceWindow = TimeSpan.FromMilliseconds(
            Math.Max(0, options.Value.CoalesceWindowMilliseconds));
        _queueCapacity = Math.Max(1, options.Value.SubscriberQueueCapacity);
        _dispatchLoop = Task.Run(DispatchLoopAsync);
    }

    public long Publish(CmsModule module, string? entityId, CmsChangeOperation operation)
    {
        var version = Interlocked.Increment(ref _versions[(int)module]);
        var change = new CmsChangeEvent(module, entityId, operation, version, DateTime.UtcNow);

        _pending[module.ToString()] = change;
        if (_flushSignal.CurrentCount == 0)
        {
            try { _flushSignal.Release(); }
            catch (SemaphoreFullException) { }
        }

        return version;
    }

    public long GetVersion(CmsModule module) => Volatile.Read(ref _versions[(int)module]);

    public IReadOnlyDictionary<CmsModule, long> GetVersions() =>
        Enum.GetValues<CmsModule>().ToDictionary(module => module, GetVersion);

    public async IAsyncEnumerable<CmsChangeEvent> SubscribeAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var id = Guid.NewGuid();
        var channel = Channel.CreateBounded<CmsChangeEvent>(new BoundedChannelOptions(_queueCapacity)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        });

        _subscribers[id] = channel;

        try
        {
            await foreach (var change in channel.Reader.ReadAllAsync(cancellationToken))
                yield return change;
        }
        finally
        {
            _subscribers.TryRemove(id, out _);
            channel.Writer.TryComplete();
        }
    }

    private async Task DispatchLoopAsync()
    {
        try
        {
            while (!_cts.IsCancellationRequested)
            {
                await _flushSignal.WaitAsync(_cts.Token);

                if (_coalesceWindow > TimeSpan.Zero)
                    await Task.Delay(_coalesceWindow, _cts.Token);

                if (_pending.IsEmpty) continue;

                var keys = _pending.Keys.ToArray();
                foreach (var key in keys)
                {
                    if (_pending.TryRemove(key, out var change))
                        Broadcast(change);
                }
            }
        }
        catch (OperationCanceledException) when (_cts.IsCancellationRequested)
        {
            // Apagado normal del host.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "El despachador de cambios del panel se detuvo.");
        }
    }

    private void Broadcast(CmsChangeEvent change)
    {
        foreach (var (id, channel) in _subscribers)
        {
            if (!channel.Writer.TryWrite(change))
            {
                // La cola está cerrada o el suscriptor ya no existe.
                _subscribers.TryRemove(id, out _);
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();

        foreach (var channel in _subscribers.Values)
            channel.Writer.TryComplete();

        try { await _dispatchLoop; }
        catch (OperationCanceledException) { }

        _flushSignal.Dispose();
        _cts.Dispose();
    }
}
