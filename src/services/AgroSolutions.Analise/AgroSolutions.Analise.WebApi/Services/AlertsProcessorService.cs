using AgroSolutions.Analise.WebApi.Infrastructure.Observability;

namespace AgroSolutions.Analise.WebApi.Services;

public class AlertsProcessorService : BackgroundService
{
    private readonly ILogger<AlertsProcessorService> _logger;
    private readonly AnaliseMetrics _metrics;
    private readonly Random _random = new();

    public AlertsProcessorService(ILogger<AlertsProcessorService> logger, AnaliseMetrics metrics)
    {
        _logger = logger;
        _metrics = metrics;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Motor de Alertas iniciado (Simulação).");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Simula intervalo entre mensagens
                await Task.Delay(_random.Next(500, 2000), stoppingToken);

                // Simula processamento
                var startTime = DateTime.UtcNow;
                
                // Simula tempo de trabalho variado para gerar dados para o histograma
                var processingMs = _random.Next(50, 500);
                if (_random.NextDouble() > 0.95) processingMs += 1000; // 5% de outliers

                await Task.Delay(processingMs, stoppingToken);

                var duration = (DateTime.UtcNow - startTime).TotalSeconds;
                _metrics.AlertProcessingDuration.Record(duration);
                
                _logger.LogDebug("Alerta processado em {Duration}s", duration);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no processamento de alertas");
            }
        }
    }
}
