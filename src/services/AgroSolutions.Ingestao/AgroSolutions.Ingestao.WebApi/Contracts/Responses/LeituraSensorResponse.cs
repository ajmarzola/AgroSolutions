using System.Text.Json.Serialization;

namespace AgroSolutions.Ingestao.WebApi.Contracts.Responses;

public sealed class LeituraSensorResponse
{
    [JsonPropertyName("id")]
    public long Id { get; init; }

    [JsonPropertyName("idPropriedade")]
    public Guid IdPropriedade { get; init; }

    [JsonPropertyName("idTalhao")]
    public Guid IdTalhao { get; init; }

    [JsonPropertyName("origem")]
    public string Origem { get; init; } = default!;

    [JsonPropertyName("dataHoraCapturaUtc")]
    public DateTime DataHoraCapturaUtc { get; init; }

    [JsonPropertyName("metricas")]
    public MetricasSensorResponse Metricas { get; init; } = new();

    [JsonPropertyName("meta")]
    public MetaLeituraResponse? Meta { get; init; }
}

public sealed class MetricasSensorResponse
{
    [JsonPropertyName("umidadeSoloPercentual")]
    public decimal? UmidadeSoloPercentual { get; init; }

    [JsonPropertyName("temperaturaCelsius")]
    public decimal? TemperaturaCelsius { get; init; }

    [JsonPropertyName("precipitacaoMilimetros")]
    public decimal? PrecipitacaoMilimetros { get; init; }
}

public sealed class MetaLeituraResponse
{
    [JsonPropertyName("idDispositivo")]
    public string? IdDispositivo { get; init; }

    [JsonPropertyName("correlationId")]
    public string? CorrelationId { get; init; }
}
