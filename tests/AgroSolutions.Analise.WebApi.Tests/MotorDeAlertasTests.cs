using AgroSolutions.Analise.WebApi.Domain;
using AgroSolutions.Analise.WebApi.Infrastructure.Repositorios;
using AgroSolutions.Analise.WebApi.Services;
using Moq;
using Xunit;

namespace AgroSolutions.Analise.WebApi.Tests;

public class MotorDeAlertasTests
{
    private readonly Mock<IAnaliseRepositorio> _repoMock;
    private readonly MotorDeAlertas _motor;

    public MotorDeAlertasTests()
    {
        _repoMock = new Mock<IAnaliseRepositorio>();
        _repoMock.Setup(r => r.GetLeiturasUltimas24HorasAsync(It.IsAny<Guid>()))
                 .ReturnsAsync(new List<Leitura>());
        _motor = new MotorDeAlertas(_repoMock.Object);
    }

    [Fact]
    public async Task Temperatura_MaiorQue35_GeraAlertaCritico()
    {
        // Arrange
        var leitura = new Leitura { TemperaturaCelsius = 36, IdTalhao = Guid.NewGuid() };

        // Act
        var alertas = await _motor.AvaliarLeituraAsync(leitura);

        // Assert
        Assert.Single(alertas);
        var alerta = alertas.First();
        Assert.Equal("Critical", alerta.Nivel);
        Assert.Contains("Temperatura Crítica", alerta.Mensagem);
    }

    [Fact]
    public async Task Temperatura_MenorQueZero_GeraAlertaWarning()
    {
        // Arrange
        var leitura = new Leitura { TemperaturaCelsius = -1, IdTalhao = Guid.NewGuid() };

        // Act
        var alertas = await _motor.AvaliarLeituraAsync(leitura);

        // Assert
        Assert.Single(alertas);
        var alerta = alertas.First();
        Assert.Equal("Warning", alerta.Nivel);
        Assert.Contains("Risco de Geada", alerta.Mensagem);
    }

    [Fact]
    public async Task Umidade_MenorQue20_GeraAlertaCritico()
    {
        // Arrange
        var leitura = new Leitura { UmidadeSoloPercentual = 15, IdTalhao = Guid.NewGuid() };

        // Act
        var alertas = await _motor.AvaliarLeituraAsync(leitura);

        // Assert
        Assert.Single(alertas);
        var alerta = alertas.First();
        Assert.Equal("Critical", alerta.Nivel);
        Assert.Contains("Seca Extrema", alerta.Mensagem);
    }

    [Fact]
    public async Task LeituraNormal_NaoGeraAlerta()
    {
        // Arrange
        var leitura = new Leitura { TemperaturaCelsius = 25, UmidadeSoloPercentual = 50, IdTalhao = Guid.NewGuid() };

        // Act
        var alertas = await _motor.AvaliarLeituraAsync(leitura);

        // Assert
        Assert.Empty(alertas);
    }

    [Fact]
    public async Task Umidade_Abaixo30_Por24h_GeraAlertaRiscoSeca()
    {
        // Arrange
        var idTalhao = Guid.NewGuid();
        var leitura = new Leitura { UmidadeSoloPercentual = 29, IdTalhao = idTalhao };
        
        var historico = new List<Leitura>
        {
            new Leitura { UmidadeSoloPercentual = 28, IdTalhao = idTalhao },
            new Leitura { UmidadeSoloPercentual = 25, IdTalhao = idTalhao }
        };

        _repoMock.Setup(r => r.GetLeiturasUltimas24HorasAsync(idTalhao))
                 .ReturnsAsync(historico);

        // Act
        var alertas = await _motor.AvaliarLeituraAsync(leitura);

        // Assert
        Assert.Contains(alertas, a => a.Mensagem.Contains("Risco de Seca"));
    }
}
