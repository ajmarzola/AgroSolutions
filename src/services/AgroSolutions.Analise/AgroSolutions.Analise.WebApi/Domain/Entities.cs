namespace AgroSolutions.Analise.WebApi.Domain;

public class Leitura
{
    public long Id { get; set; }
    public Guid IdTalhao { get; set; }
    public DateTime DataHoraCapturaUtc { get; set; }
    public decimal? TemperaturaCelsius { get; set; }
    public decimal? UmidadeSoloPercentual { get; set; }
    public decimal? PrecipitacaoMilimetros { get; set; }
}

public class Alerta
{
    public long Id { get; set; }
    public Guid IdTalhao { get; set; }
    public string Mensagem { get; set; } = string.Empty;
    public string Nivel { get; set; } = "Info"; // Info, Warning, Critical
    public DateTime DataHoraGeracaoUtc { get; set; }
    public long? LeituraId { get; set; }
}
