using AgroSolutions.Analise.WebApi.Domain;
using AgroSolutions.Analise.WebApi.Services;
using Xunit;

namespace AgroSolutions.Analise.WebApi.Tests;

public class MotorDeAlertasTests
{
    private readonly MotorDeAlertas _motor;

    public MotorDeAlertasTests()
    {
        _motor = new MotorDeAlertas();
    }

    [Fact]
    public void Temperatura_MaiorQue35_GeraAlertaCritico()
    {
        // Arrange
        var leitura = new Leitura { TemperaturaCelsius = 36, IdTalhao = Guid.NewGuid() };

        // Act
        var alertas = _motor.AvaliarLeitura(leitura);

        // Assert
        Assert.Single(alertas);
        var alerta = alertas.First();
        Assert.Equal("Critical", alerta.Nivel);
        Assert.Contains("Temperatura Crítica", alerta.Mensagem);
    }

    [Fact]
    public void Temperatura_MenorQueZero_GeraAlertaWarning()
    {
        // Arrange
        var leitura = new Leitura { TemperaturaCelsius = -1, IdTalhao = Guid.NewGuid() };

        // Act
        var alertas = _motor.AvaliarLeitura(leitura);

        // Assert
        Assert.Single(alertas);
        var alerta = alertas.First();
        Assert.Equal("Warning", alerta.Nivel);
        Assert.Contains("Risco de Geada", alerta.Mensagem);
    }

    [Fact]
    public void Umidade_MenorQue20_GeraAlertaCritico()
    {
        // Arrange
        var leitura = new Leitura { UmidadeSoloPercentual = 15, IdTalhao = Guid.NewGuid() };

        // Act
        var alertas = _motor.AvaliarLeitura(leitura);

        // Assert
        Assert.Single(alertas);
        var alerta = alertas.First();
        Assert.Equal("Critical", alerta.Nivel);
        Assert.Contains("Seca Extrema", alerta.Mensagem);
    }

    [Fact]
    public void LeituraNormal_NaoGeraAlerta()
    {
        // Arrange
        var leitura = new Leitura { TemperaturaCelsius = 25, UmidadeSoloPercentual = 50, IdTalhao = Guid.NewGuid() };

        // Act
        var alertas = _motor.AvaliarLeitura(leitura);

        // Assert
        Assert.Empty(alertas);
    }
}
