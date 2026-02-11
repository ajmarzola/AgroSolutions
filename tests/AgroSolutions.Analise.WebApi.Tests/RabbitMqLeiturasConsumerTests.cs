using AgroSolutions.Analise.WebApi.Contracts;
using AgroSolutions.Analise.WebApi.Domain;
using AgroSolutions.Analise.WebApi.Infrastructure.Mensageria;
using AgroSolutions.Analise.WebApi.Infrastructure.Observability;
using AgroSolutions.Analise.WebApi.Infrastructure.Repositorios;
using AgroSolutions.Analise.WebApi.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Text.Json;
using Xunit;

using System.Diagnostics.Metrics;

namespace AgroSolutions.Analise.WebApi.Tests;

public class RabbitMqLeiturasConsumerTests
{
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock;
    private readonly Mock<IServiceScope> _scopeMock;
    private readonly Mock<IAnaliseRepositorio> _repositorioMock;
    private readonly Mock<IMotorDeAlertas> _motorMock;
    private readonly Mock<ILogger<RabbitMqLeiturasConsumer>> _loggerMock;
    private readonly Mock<IMeterFactory> _meterFactoryMock;
    private readonly AnaliseMetrics _metrics;

    public RabbitMqLeiturasConsumerTests()
    {
        _serviceProviderMock = new Mock<IServiceProvider>();
        _scopeFactoryMock = new Mock<IServiceScopeFactory>();
        _scopeMock = new Mock<IServiceScope>();
        _repositorioMock = new Mock<IAnaliseRepositorio>();
        _motorMock = new Mock<IMotorDeAlertas>();
        _loggerMock = new Mock<ILogger<RabbitMqLeiturasConsumer>>();
        _meterFactoryMock = new Mock<IMeterFactory>();
        
        // Setup MeterFactory
        _meterFactoryMock.Setup(x => x.Create(It.IsAny<MeterOptions>())).Returns(new Meter("TestMeter"));
        // Remove the overload setup that was causing issues or simplify
        

        _metrics = new AnaliseMetrics(_meterFactoryMock.Object);       

        // Setup Scope Mock
        _scopeFactoryMock.Setup(x => x.CreateScope()).Returns(_scopeMock.Object);
        _scopeMock.Setup(x => x.ServiceProvider).Returns(_serviceProviderMock.Object);
        _serviceProviderMock.Setup(x => x.GetService(typeof(IServiceScopeFactory))).Returns(_scopeFactoryMock.Object);
        
        // Setup Dependency Resolution
        _serviceProviderMock.Setup(x => x.GetService(typeof(IAnaliseRepositorio))).Returns(_repositorioMock.Object);
        _serviceProviderMock.Setup(x => x.GetService(typeof(IMotorDeAlertas))).Returns(_motorMock.Object);
    }

    [Fact]
    public async Task ProcessarMensagemAsync_DeveSalvarLeitura_E_GerarAlertas()
    {
        // Arrange
        var options = Options.Create(new RabbitMqOptions { Enabled = false }); // Disabled to avoid connection attempts in constructor
        
        // Need to construct consumer. But ProcessarMensagemAsync is private/protected? 
        // We need to access it via reflection or if we made it internal and added InternalsVisibleTo.
        
        var consumer = new RabbitMqLeiturasConsumer(options, _serviceProviderMock.Object, _loggerMock.Object, _metrics);

        // Payload
        var evento = new LeituraSensorEvent
        {
            Leitura = new LeituraPayload 
            { 
                IdTalhao = Guid.NewGuid(),
                Metricas = new MetricasPayload { TemperaturaCelsius = 40 }
            }
        };
        var json = JsonSerializer.Serialize(evento);

        // Setup Motor behavior
        _motorMock.Setup(m => m.AvaliarLeituraAsync(It.IsAny<Leitura>()))
            .ReturnsAsync(new List<Alerta> { new Alerta { Mensagem = "Teste" } });

        // Act
        // Invoke internal method 'ProcessarMensagemAsync'
        var method = typeof(RabbitMqLeiturasConsumer)
            .GetMethod("ProcessarMensagemAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (method == null) throw new Exception("Method ProcessarMensagemAsync not found");

        await (Task)method.Invoke(consumer, new object[] { json })!;

        // Assert
        _repositorioMock.Verify(r => r.SalvarLeituraAsync(It.IsAny<Leitura>()), Times.Once);
        _motorMock.Verify(m => m.AvaliarLeituraAsync(It.IsAny<Leitura>()), Times.Once);
        _repositorioMock.Verify(r => r.SalvarAlertaAsync(It.IsAny<Alerta>()), Times.Once);
    }
}
