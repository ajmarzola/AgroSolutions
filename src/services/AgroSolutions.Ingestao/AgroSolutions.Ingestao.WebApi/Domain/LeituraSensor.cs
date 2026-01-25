namespace AgroSolutions.Ingestao.WebApi.Domain;

public sealed class LeituraSensor
{
    public long Id { get; set; }

    public Guid IdPropriedade { get; set; }

    public Guid IdTalhao { get; set; }

    public string Origem { get; set; } = "simulador";

    public DateTime DataHoraCapturaUtc { get; set; }

    public decimal? UmidadeSoloPercentual { get; set; }

    public decimal? TemperaturaCelsius { get; set; }

    public decimal? PrecipitacaoMilimetros { get; set; }

    public string? IdDispositivo { get; set; }

    public string? CorrelationId { get; set; }
}
