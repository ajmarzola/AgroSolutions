using System.Diagnostics.Metrics;

namespace AgroSolutions.Analise.WebApi.Infrastructure.Observability;

public class AnaliseMetrics
{
    public const string MeterName = "AgroSolutions.Analise";
    private readonly Meter _meter;

    public Histogram<double> AlertProcessingDuration { get; }

    public AnaliseMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create(MeterName);
        AlertProcessingDuration = _meter.CreateHistogram<double>("agrosolutions_alerts_processing_duration_seconds", unit: "s", description: "Tempo de processamento do motor de alertas");
    }
}
