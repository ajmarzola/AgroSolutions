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
    private readonly IngestaoMetrics _metrics;
    private readonly Policy _retryPolicy;
    private readonly ConnectionFactory _factory;
    private readonly object _lock = new object();

    private IConnection? _connection;
    private IModel? _channel;

    public RabbitMqEventoPublisher(IOptions<RabbitMqOptions> options, IngestaoMetrics metrics)
    {
        _options = options.Value;
        _metrics = metrics;

        _factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password,
            DispatchConsumersAsync = true,
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(5)
        };
        
        _retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetry(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), 
                (exception, timeSpan, retryCount, context) => {
                    _metrics.RabbitMqErrorsTotal.Add(1,
                         new KeyValuePair<string, object?>("operation", "publish_retry"),
                         new KeyValuePair<string, object?>("queue_name", _options.RoutingKeyLeituraRecebida));
                });

        try
        {
            Connect();
        }
        catch
        {
            // Ignorar falha na inicialização para não derrubar a API
        }
    }

    private void Connect()
    {
        lock (_lock)
        {
            if (_connection != null && _connection.IsOpen)
            {
                return;
            }

            var connectPolicy = Policy.Handle<Exception>()
                .WaitAndRetry(2, r => TimeSpan.FromSeconds(1));

            _connection = connectPolicy.Execute(() => _factory.CreateConnection());
            _channel = _connection.CreateModel();

            _channel.ExchangeDeclare(exchange: _options.Exchange, type: ExchangeType.Topic, durable: true, autoDelete: false);
            _channel.ConfirmSelect(); 
        }
    }

    public Task PublicarLeituraRecebidaAsync(LeituraSensor leitura, CancellationToken ct)
    {
        try
        {
            if (_connection == null || !_connection.IsOpen)
            {
                Connect();
            }

            if (_channel == null || _channel.IsClosed)
            {
                throw new Exception("RabbitMQ Channel not initialized.");
            }

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
        try { _channel?.Close(); } catch { /* ignore */ }
        try { _connection?.Close(); } catch { /* ignore */ }
        _channel?.Dispose();
        _connection?.Dispose();
    }
}
