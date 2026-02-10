namespace AgroSolutions.Analise.WebApi.Infrastructure.Mensageria;

public class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string Exchange { get; set; } = "agrosolutions";
    public string QueueAnalise { get; set; } = "AgroSolutions.Analise.Leituras";
    public string RoutingKeyLeituraRecebida { get; set; } = "ingestao.leitura_sensor_recebida";
    public bool Enabled { get; set; } = true;
}
