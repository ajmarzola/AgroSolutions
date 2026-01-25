using AgroSolutions.Ingestao.WebApi.Contracts.Requests;
using AgroSolutions.Ingestao.WebApi.Contracts.Responses;
using AgroSolutions.Ingestao.WebApi.Controllers;
using AgroSolutions.Ingestao.WebApi.Domain;
using AgroSolutions.Ingestao.WebApi.Infrastructure.Mensageria;
using AgroSolutions.Ingestao.WebApi.Infrastructure.Repositorios;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AgroSolutions.Ingestao.WebApi.Tests;

public class LeiturasSensoresControllerTests
{
    private readonly Mock<ILeituraSensorRepositorio> _repositorioMock;
    private readonly Mock<IEventoPublisher> _publisherMock;
    private readonly Mock<ILogger<LeiturasSensoresController>> _loggerMock;
    private readonly LeiturasSensoresController _controller;

    public LeiturasSensoresControllerTests()
    {
        _repositorioMock = new Mock<ILeituraSensorRepositorio>();
        _publisherMock = new Mock<IEventoPublisher>();
        _loggerMock = new Mock<ILogger<LeiturasSensoresController>>();

        _controller = new LeiturasSensoresController(
            _repositorioMock.Object,
            _publisherMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task CriarAsync_DeveRetornarBadRequest_QuandoNenhumaMetricaForInformada()
    {
        // Arrange
        var request = new CriarLeituraSensorRequest
        {
            Metricas = new MetricasSensorRequest
            {
                UmidadeSoloPercentual = null,
                TemperaturaCelsius = null,
                PrecipitacaoMilimetros = null
            }
        };

        // Act
        var result = await _controller.CriarAsync(request, CancellationToken.None);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        var problemDetails = Assert.IsType<ValidationProblemDetails>(objectResult.Value);
        Assert.Contains("metricas", problemDetails.Errors.Keys);
    }

    [Fact]
    public async Task CriarAsync_DeveRetornarCreated_QuandoRequestValido()
    {
        // Arrange
        var request = new CriarLeituraSensorRequest
        {
            IdPropriedade = Guid.NewGuid(),
            IdTalhao = Guid.NewGuid(),
            Origem = "teste",
            DataHoraCapturaUtc = DateTime.UtcNow,
            Metricas = new MetricasSensorRequest
            {
                TemperaturaCelsius = 25.5m
            },
            Meta = new MetaLeituraRequest
            {
                IdDispositivo = "disp-01",
                CorrelationId = "corr-01"
            }
        };

        _repositorioMock.Setup(x => x.InserirAsync(It.IsAny<LeituraSensor>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(123);

        // Act
        var result = await _controller.CriarAsync(request, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(LeiturasSensoresController.ConsultarAsync), createdResult.ActionName);
        Assert.NotNull(createdResult.RouteValues);
        Assert.True(createdResult.RouteValues.ContainsKey("idTalhao"));
        
        // Verificando o corpo da resposta
        var body = createdResult.Value;
        Assert.NotNull(body);
        var idProperty = body.GetType().GetProperty("id");
        Assert.NotNull(idProperty);
        Assert.Equal(123L, idProperty.GetValue(body));

        _repositorioMock.Verify(x => x.InserirAsync(It.Is<LeituraSensor>(l =>
            l.IdPropriedade == request.IdPropriedade &&
            l.IdTalhao == request.IdTalhao &&
            l.Origem == request.Origem &&
            l.TemperaturaCelsius == request.Metricas.TemperaturaCelsius &&
            l.IdDispositivo == request.Meta.IdDispositivo &&
            l.CorrelationId == request.Meta.CorrelationId
        ), It.IsAny<CancellationToken>()), Times.Once);

        _publisherMock.Verify(x => x.PublicarLeituraRecebidaAsync(It.Is<LeituraSensor>(l => l.Id == 123), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConsultarAsync_DeveRetornarBadRequest_QuandoIdTalhaoEstiverVazio()
    {
        // Act
        var result = await _controller.ConsultarAsync(Guid.Empty, DateTime.UtcNow.AddHours(-1), DateTime.UtcNow, null, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("idTalhao é obrigatório.", badRequestResult.Value);
    }

    [Fact]
    public async Task ConsultarAsync_DeveRetornarBadRequest_QuandoDatasInvalidas()
    {
        // Act
        var result = await _controller.ConsultarAsync(Guid.NewGuid(), default, DateTime.UtcNow, null, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Intervalo inválido. Informe deUtc e ateUtc (ateUtc > deUtc).", badRequestResult.Value);
    }

    [Fact]
    public async Task ConsultarAsync_DeveRetornarBadRequest_QuandoAteMenorQueDe()
    {
        // Arrange
        var de = DateTime.UtcNow;
        var ate = de.AddMinutes(-10);

        // Act
        var result = await _controller.ConsultarAsync(Guid.NewGuid(), de, ate, null, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Intervalo inválido. Informe deUtc e ateUtc (ateUtc > deUtc).", badRequestResult.Value);
    }

    [Fact]
    public async Task ConsultarAsync_DeveRetornarOk_QuandoParametrosValidos()
    {
        // Arrange
        var idTalhao = Guid.NewGuid();
        var de = DateTime.UtcNow.AddHours(-1);
        var ate = DateTime.UtcNow;
        var dadosEsperados = new List<LeituraSensor>
        {
            new LeituraSensor { Id = 1, IdTalhao = idTalhao, TemperaturaCelsius = 20, DataHoraCapturaUtc = de.AddMinutes(10) },
            new LeituraSensor { Id = 2, IdTalhao = idTalhao, TemperaturaCelsius = 22, DataHoraCapturaUtc = de.AddMinutes(20) }
        };

        _repositorioMock.Setup(x => x.ConsultarAsync(idTalhao, It.IsAny<DateTime>(), It.IsAny<DateTime>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dadosEsperados);

        // Act
        var result = await _controller.ConsultarAsync(idTalhao, de, ate, null, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsAssignableFrom<IReadOnlyList<LeituraSensorResponse>>(okResult.Value);
        Assert.Equal(2, response.Count);
        Assert.Equal(1, response[0].Id);
        Assert.Equal(2, response[1].Id);
    }

    [Fact]
    public async Task CriarAsync_DeveFuncionar_QuandoMetaForNulo()
    {
        // Arrange
        var request = new CriarLeituraSensorRequest
        {
            IdPropriedade = Guid.NewGuid(),
            IdTalhao = Guid.NewGuid(),
            DataHoraCapturaUtc = DateTime.UtcNow,
            Metricas = new MetricasSensorRequest
            {
                TemperaturaCelsius = 20
            },
            Meta = null
        };

        _repositorioMock.Setup(x => x.InserirAsync(It.IsAny<LeituraSensor>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _controller.CriarAsync(request, CancellationToken.None);

        // Assert
        Assert.IsType<CreatedAtActionResult>(result);
        _repositorioMock.Verify(x => x.InserirAsync(It.Is<LeituraSensor>(l => l.IdDispositivo == null && l.CorrelationId == null), It.IsAny<CancellationToken>()), Times.Once);
    }
}

