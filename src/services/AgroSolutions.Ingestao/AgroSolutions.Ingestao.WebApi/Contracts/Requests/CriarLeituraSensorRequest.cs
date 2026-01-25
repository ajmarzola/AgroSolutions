using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AgroSolutions.Ingestao.WebApi.Contracts.Requests;

public sealed class CriarLeituraSensorRequest
{
    [Required]
    [JsonPropertyName("idPropriedade")]
    public Guid IdPropriedade { get; init; }

    [Required]
    [JsonPropertyName("idTalhao")]
    public Guid IdTalhao { get; init; }

    [Required]
    [MaxLength(30)]
    [JsonPropertyName("origem")]
    public string Origem { get; init; } = "simulador";

    [Required]
    [JsonPropertyName("dataHoraCapturaUtc")]
    public DateTime DataHoraCapturaUtc { get; init; }

    [Required]
    [JsonPropertyName("metricas")]
    public MetricasSensorRequest Metricas { get; init; } = new();

    [JsonPropertyName("meta")]
    public MetaLeituraRequest? Meta { get; init; }
}

public sealed class MetricasSensorRequest
{
    [JsonPropertyName("umidadeSoloPercentual")]
    [Range(0, 100)]
    public decimal? UmidadeSoloPercentual { get; init; }

    [JsonPropertyName("temperaturaCelsius")]
    [Range(-60, 80)]
    public decimal? TemperaturaCelsius { get; init; }

    [JsonPropertyName("precipitacaoMilimetros")]
    [Range(0, 1000)]
    public decimal? PrecipitacaoMilimetros { get; init; }
}

public sealed class MetaLeituraRequest
{
    [MaxLength(50)]
    [JsonPropertyName("idDispositivo")]
    public string? IdDispositivo { get; init; }

    [MaxLength(100)]
    [JsonPropertyName("correlationId")]
    public string? CorrelationId { get; init; }
}
