namespace Aplication.Services;

/// <summary>
/// Coordinación en memoria del worker de automatización: señal de ejecución manual,
/// bloqueo para evitar corridas concurrentes y cancelación de la corrida en curso.
/// Registrar como singleton.
/// </summary>
public sealed class AutomationCoordinator
{
    private readonly SemaphoreSlim _signal = new(0, 1);
    private readonly SemaphoreSlim _runLock = new(1, 1);
    private readonly object _ctsLock = new();
    private volatile bool _running;
    private volatile bool _pending;
    private CancellationTokenSource? _runCts;

    /// <summary>Indica si hay una corrida en curso o una solicitud de corrida aún no iniciada.</summary>
    public bool IsBusy => _running || _pending;

    /// <summary>Señala una ejecución manual. Se ignora si ya hay una señal pendiente.</summary>
    public void SignalRun()
    {
        _pending = true;
        if (_signal.CurrentCount == 0)
        {
            try { _signal.Release(); }
            catch (SemaphoreFullException) { }
        }
    }

    /// <summary>
    /// Espera la señal manual o el intervalo indicado. Devuelve <c>true</c> si llegó la señal
    /// (ejecución manual) y <c>false</c> si expiró el intervalo (ejecución programada).
    /// </summary>
    public Task<bool> WaitForSignalAsync(TimeSpan timeout, CancellationToken ct) =>
        _signal.WaitAsync(timeout, ct);

    /// <summary>Intenta adquirir el bloqueo de corrida sin bloquear. <c>false</c> si ya hay una corrida en curso.</summary>
    public async Task<bool> TryBeginRunAsync(CancellationToken ct)
    {
        var acquired = await _runLock.WaitAsync(0, ct);
        if (acquired)
        {
            _pending = false;
            _running = true;
            lock (_ctsLock)
            {
                _runCts?.Dispose();
                _runCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            }
        }

        return acquired;
    }

    /// <summary>Token que se cancela al solicitar la detención de la corrida en curso.</summary>
    public CancellationToken RunToken
    {
        get
        {
            lock (_ctsLock)
            {
                return _runCts?.Token ?? CancellationToken.None;
            }
        }
    }

    /// <summary>Solicita detener la corrida en curso. Devuelve <c>true</c> si había una activa.</summary>
    public bool RequestStop()
    {
        lock (_ctsLock)
        {
            if (_runCts is null) return false;

            try
            {
                _runCts.Cancel();
                return true;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
        }
    }

    public void EndRun()
    {
        lock (_ctsLock)
        {
            _runCts?.Dispose();
            _runCts = null;
        }

        _running = false;
        _runLock.Release();
    }
}
