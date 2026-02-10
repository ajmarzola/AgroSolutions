namespace AgroSolutions.Analise.WebApi.Contracts;

public class LeituraSensorEvent
{
    public string EventType { get; set; } = string.Empty;
    public Guid EventId { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public LeituraPayload? Leitura { get; set; }
}

public class LeituraPayload
{
    public long Id { get; set; }
    public Guid IdPropriedade { get; set; }
    public Guid IdTalhao { get; set; }
    public string Origem { get; set; } = string.Empty;
    public DateTime DataHoraCapturaUtc { get; set; }
    public MetricasPayload? Metricas { get; set; }
    public MetaPayload? Meta { get; set; }
}

public class MetricasPayload
{
    public decimal? UmidadeSoloPercentual { get; set; }
    public decimal? TemperaturaCelsius { get; set; }
    public decimal? PrecipitacaoMilimetros { get; set; }
}

public class MetaPayload
{
    public string? IdDispositivo { get; set; }
    public string? CorrelationId { get; set; }
}
