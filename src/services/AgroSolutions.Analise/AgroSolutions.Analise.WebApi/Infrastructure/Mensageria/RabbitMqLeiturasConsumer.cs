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
    private IChannel? _channel;

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

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Consumer RabbitMQ desabilitado.");
            return;
        }

        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password,
            AutomaticRecoveryEnabled = true
        };

        try 
        {
            var policy = Policy.Handle<Exception>()
                .WaitAndRetryAsync(5, r => TimeSpan.FromSeconds(Math.Pow(2, r)), (ex, time) => 
                {
                    _logger.LogWarning(ex, "Falha ao conectar ao RabbitMQ, retentando em {Seconds}s...", time.TotalSeconds);
                });

            _connection = await policy.ExecuteAsync(async () => await factory.CreateConnectionAsync());
            _channel = await _connection.CreateChannelAsync();

            await _channel.ExchangeDeclareAsync(_options.Exchange, ExchangeType.Topic, durable: true, autoDelete: false);
            await _channel.QueueDeclareAsync(_options.QueueAnalise, durable: true, exclusive: false, autoDelete: false);
            await _channel.QueueBindAsync(_options.QueueAnalise, _options.Exchange, _options.RoutingKeyLeituraRecebida); // Bind correto

            await _channel.BasicQosAsync(0, 10, false); // PrefetchCount = 10

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (sender, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                try
                {
                    await ProcessarMensagemAsync(message);
                    await _channel.BasicAckAsync(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao processar mensagem. Rejeitando.");
                    // Em produção, considerar estratégia de Dead Letter Queue (DLQ)
                    await _channel.BasicNackAsync(ea.DeliveryTag, false, requeue: false); 
                }
            };

            await _channel.BasicConsumeAsync(queue: _options.QueueAnalise, autoAck: false, consumer: consumer);
            _logger.LogInformation("Consumer RabbitMQ iniciado na fila {Queue}", _options.QueueAnalise);
        }
        catch (Exception ex)
        {
             _logger.LogCritical(ex, "Não foi possível iniciar o consumer RabbitMQ.");
        }

        return;
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
            var alertas = await motorAlertas.AvaliarLeituraAsync(leitura);
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
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}
