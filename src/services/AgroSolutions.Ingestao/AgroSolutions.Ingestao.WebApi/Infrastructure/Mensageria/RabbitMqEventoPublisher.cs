using System.Text;
using System.Text.Json;
using AgroSolutions.Ingestao.WebApi.Domain;
using AgroSolutions.Ingestao.WebApi.Infrastructure.Observability;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Polly;

namespace AgroSolutions.Ingestao.WebApi.Infrastructure.Mensageria;

public sealed class RabbitMqEventoPublisher : IEventoPublisher, IDisposable
{
    private readonly RabbitMqOptions _options;
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly IngestaoMetrics _metrics;
    private readonly Policy _retryPolicy;

    public RabbitMqEventoPublisher(IOptions<RabbitMqOptions> options, IngestaoMetrics metrics)
    {
        _options = options.Value;
        _metrics = metrics;

        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password,
            DispatchConsumersAsync = true,
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(5)
        };
        
        // Retry connection roughly
        var connectPolicy = Policy.Handle<Exception>()
            .WaitAndRetry(5, r => TimeSpan.FromSeconds(Math.Pow(2, r)));

        _connection = connectPolicy.Execute(() => factory.CreateConnection());
        _channel = _connection.CreateModel();

        _channel.ExchangeDeclare(exchange: _options.Exchange, type: ExchangeType.Topic, durable: true, autoDelete: false);
        _channel.ConfirmSelect(); // Publisher Confirms

        _retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetry(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), 
                (exception, timeSpan, retryCount, context) => {
                    _metrics.RabbitMqErrorsTotal.Add(1,
                         new KeyValuePair<string, object?>("operation", "publish_retry"),
                         new KeyValuePair<string, object?>("queue_name", _options.RoutingKeyLeituraRecebida));
                });
    }

    public Task PublicarLeituraRecebidaAsync(LeituraSensor leitura, CancellationToken ct)
    {
        // ct não é suportado nativamente pelo client; mantemos assinatura para consistência.
        try
        {
            var evento = new
            {
                eventType = "LeituraSensorRecebida",
                eventId = Guid.NewGuid(),
                occurredAtUtc = DateTime.UtcNow,
                leitura = new
                {
                    id = leitura.Id,
                    idPropriedade = leitura.IdPropriedade,
                    idTalhao = leitura.IdTalhao,
                    origem = leitura.Origem,
                    dataHoraCapturaUtc = leitura.DataHoraCapturaUtc,
                    metricas = new
                    {
                        umidadeSoloPercentual = leitura.UmidadeSoloPercentual,
                        temperaturaCelsius = leitura.TemperaturaCelsius,
                        precipitacaoMilimetros = leitura.PrecipitacaoMilimetros
                    },
                    meta = new
                    {
                        idDispositivo = leitura.IdDispositivo,
                        correlationId = leitura.CorrelationId
                    }
                }
            };

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(evento));
            var props = _channel.CreateBasicProperties();
            props.Persistent = true;
            props.ContentType = "application/json";

            _retryPolicy.Execute(() =>
            {
                _channel.BasicPublish(
                    exchange: _options.Exchange,
                    routingKey: _options.RoutingKeyLeituraRecebida,
                    basicProperties: props,
                    body: body);
                
                _channel.WaitForConfirmsOrDie(TimeSpan.FromSeconds(5));
            });

            return Task.CompletedTask;
        }
        catch (Exception)
        {
            _metrics.RabbitMqErrorsTotal.Add(1,
                new KeyValuePair<string, object?>("operation", "publish_failed"),
                new KeyValuePair<string, object?>("queue_name", _options.RoutingKeyLeituraRecebida));
            throw;
        }
    }

    public void Dispose()
    {
        try { _channel.Close(); } catch { /* ignore */ }
        try { _connection.Close(); } catch { /* ignore */ }
        _channel.Dispose();
        _connection.Dispose();
    }
}
