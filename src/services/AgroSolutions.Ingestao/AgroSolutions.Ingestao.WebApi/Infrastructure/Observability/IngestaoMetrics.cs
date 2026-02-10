using System.Diagnostics.Metrics;

namespace AgroSolutions.Ingestao.WebApi.Infrastructure.Observability;

public class IngestaoMetrics
{
    public const string MeterName = "AgroSolutions.Ingestao";
    private readonly Meter _meter;

    public Counter<long> LeiturasTotal { get; }
    public Counter<long> RabbitMqErrorsTotal { get; }

    public IngestaoMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create(MeterName);
        LeiturasTotal = _meter.CreateCounter<long>("agrosolutions_sensor_readings_total", description: "Contador de Leituras Recebidas");
        RabbitMqErrorsTotal = _meter.CreateCounter<long>("agrosolutions_rabbitmq_errors_total", description: "Falhas de Mensageria");
    }
}
