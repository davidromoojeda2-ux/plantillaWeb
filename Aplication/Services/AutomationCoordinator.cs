namespace Aplication.Services;

/// <summary>
/// Coordinación en memoria del worker de automatización: señal de ejecución manual,
/// estado de pausa y bloqueo para evitar corridas concurrentes. Registrar como singleton.
/// </summary>
public sealed class AutomationCoordinator
{
    private readonly SemaphoreSlim _signal = new(0, 1);
    private readonly SemaphoreSlim _runLock = new(1, 1);
    private volatile bool _paused;
    private volatile bool _running;
    private volatile bool _pending;

    public bool IsPaused => _paused;

    /// <summary>Indica si hay una corrida ejecutándose en este momento.</summary>
    public bool IsRunning => _running;

    /// <summary>Indica si hay una corrida en curso o una solicitud de corrida aún no iniciada.</summary>
    public bool IsBusy => _running || _pending;

    public void SetPaused(bool paused) => _paused = paused;

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
        }

        return acquired;
    }

    public void EndRun()
    {
        _running = false;
        _runLock.Release();
    }
}
