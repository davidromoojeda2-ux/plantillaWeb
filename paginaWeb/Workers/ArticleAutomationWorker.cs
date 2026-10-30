using Aplication.Interfaces;
using Aplication.Services;
using Domine.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace paginaWeb.Workers;

/// <summary>
/// Worker embebido que (1) publica los artículos programados cuya fecha venció y
/// (2) ejecuta el pipeline de generación según la frecuencia o un disparo manual.
/// El bucle está blindado para que ningún fallo detenga la aplicación web.
/// </summary>
public sealed class ArticleAutomationWorker : BackgroundService
{
    private static readonly TimeSpan Tick = TimeSpan.FromSeconds(30);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AutomationCoordinator _coordinator;
    private readonly ILogger<ArticleAutomationWorker> _logger;

    public ArticleAutomationWorker(
        IServiceScopeFactory scopeFactory,
        AutomationCoordinator coordinator,
        ILogger<ArticleAutomationWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _coordinator = coordinator;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        DateTime? lastRun = DateTime.UtcNow;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Espera una señal manual o el siguiente tick.
                var manual = await _coordinator.WaitForSignalAsync(Tick, stoppingToken);

                var settings = await ReadSettingsAsync(stoppingToken);
                _coordinator.SetPaused(settings.Paused);

                if (!settings.Enabled) continue;

                // Publicar artículos programados cuya fecha venció.
                await PublishScheduledAsync(stoppingToken);

                var interval = TimeSpan.FromMinutes(Math.Max(1, settings.FrequencyMinutes));
                var due = DateTime.UtcNow - lastRun >= interval;

                if (manual || (due && !settings.Paused))
                {
                    lastRun = DateTime.UtcNow;
                    await RunAsync(manual ? AutomationTrigger.Manual : AutomationTrigger.Programada, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en el worker de automatización.");
            }
        }
    }

    private async Task PublishScheduledAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var automation = scope.ServiceProvider.GetRequiredService<IAutomationService>();
        var published = await automation.PublishScheduledAsync(DateTime.UtcNow, ct);
        if (published > 0)
            _logger.LogInformation("Publicados {Count} artículo(s) programado(s).", published);
    }

    private async Task RunAsync(AutomationTrigger trigger, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var automation = scope.ServiceProvider.GetRequiredService<IAutomationService>();
        await automation.RunOnceAsync(trigger, ct);
    }

    private async Task<AutomationSettings> ReadSettingsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var automation = scope.ServiceProvider.GetRequiredService<IAutomationService>();
        return await automation.GetSettingsAsync(ct);
    }
}
