using System.Text;
using System.Text.Json;
using AgroSolutions.Analise.WebApi.Contracts;
using AgroSolutions.Analise.WebApi.Domain;
using AgroSolutions.Analise.WebApi.Services;
using AgroSolutions.Analise.WebApi.Infrastructure.Observability;
using AgroSolutions.Analise.WebApi.Infrastructure.Repositorios;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Polly;

namespace AgroSolutions.Analise.WebApi.Infrastructure.Mensageria;

public class RabbitMqLeiturasConsumer : BackgroundService
{
    private readonly RabbitMqOptions _options;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RabbitMqLeiturasConsumer> _logger;
    private readonly AnaliseMetrics _metrics;
    private IConnection? _connection;
    private IModel? _channel;

    public RabbitMqLeiturasConsumer(
        IOptions<RabbitMqOptions> options,
        IServiceProvider serviceProvider,
        ILogger<RabbitMqLeiturasConsumer> logger,
        AnaliseMetrics metrics)
    {
        _options = options.Value;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _metrics = metrics;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Consumer RabbitMQ desabilitado.");
            return Task.CompletedTask;
        }

        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password,
            DispatchConsumersAsync = true,
            AutomaticRecoveryEnabled = true
        };

        try 
        {
            var policy = Policy.Handle<Exception>()
                .WaitAndRetry(5, r => TimeSpan.FromSeconds(Math.Pow(2, r)), (ex, time) => 
                {
                    _logger.LogWarning(ex, "Falha ao conectar ao RabbitMQ, retentando em {Seconds}s...", time.TotalSeconds);
                });

            _connection = policy.Execute(() => factory.CreateConnection());
            _channel = _connection.CreateModel();

            _channel.ExchangeDeclare(_options.Exchange, ExchangeType.Topic, durable: true, autoDelete: false);
            _channel.QueueDeclare(_options.QueueAnalise, durable: true, exclusive: false, autoDelete: false);
            _channel.QueueBind(_options.QueueAnalise, _options.Exchange, _options.RoutingKeyLeituraRecebida); // Bind correto

            _channel.BasicQos(0, 10, false); // PrefetchCount = 10

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                
                try
                {
                    await ProcessarMensagemAsync(message);
                    _channel.BasicAck(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao processar mensagem. Rejeitando.");
                    // Em produção, considerar estratégia de Dead Letter Queue (DLQ)
                    _channel.BasicNack(ea.DeliveryTag, false, requeue: false); 
                }
            };

            _channel.BasicConsume(queue: _options.QueueAnalise, autoAck: false, consumer: consumer);
            _logger.LogInformation("Consumer RabbitMQ iniciado na fila {Queue}", _options.QueueAnalise);
        }
        catch (Exception ex)
        {
             _logger.LogCritical(ex, "Não foi possível iniciar o consumer RabbitMQ.");
        }

        return Task.CompletedTask;
    }

    private async Task ProcessarMensagemAsync(string json)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var evento = JsonSerializer.Deserialize<LeituraSensorEvent>(json, options);

        if (evento?.Leitura == null)
        {
            _logger.LogWarning("Mensagem inválida ou payload nulo recebido.");
            return;
        }

        // Conversão para DTO de Domínio
        var leitura = new Leitura
        {
            IdTalhao = evento.Leitura.IdTalhao,
            DataHoraCapturaUtc = evento.Leitura.DataHoraCapturaUtc,
            TemperaturaCelsius = evento.Leitura.Metricas?.TemperaturaCelsius,
            UmidadeSoloPercentual = evento.Leitura.Metricas?.UmidadeSoloPercentual,
            PrecipitacaoMilimetros = evento.Leitura.Metricas?.PrecipitacaoMilimetros
        };

        using (var scope = _serviceProvider.CreateScope())
        {
            var repositorio = scope.ServiceProvider.GetRequiredService<IAnaliseRepositorio>();
            var motorAlertas = scope.ServiceProvider.GetRequiredService<IMotorDeAlertas>();

            // 1. Salvar Leitura
            await repositorio.SalvarLeituraAsync(leitura);

            // 2. Motor de Alertas
            var alertas = motorAlertas.AvaliarLeitura(leitura);
            foreach (var alerta in alertas)
            {
                await repositorio.SalvarAlertaAsync(alerta);
                _logger.LogWarning("ALERTA GERADO: {Mensagem} (Talhao: {Talhao})", alerta.Mensagem, alerta.IdTalhao);
                
                // Métrica de Alerta
                _metrics.AlertProcessingDuration.Record(1); 
            }
        }
    }

    public override void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        base.Dispose();
    }
}
