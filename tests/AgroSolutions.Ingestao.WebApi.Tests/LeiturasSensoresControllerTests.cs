using System.Diagnostics.Metrics;
using AgroSolutions.Ingestao.WebApi.Contracts.Requests;
using AgroSolutions.Ingestao.WebApi.Contracts.Responses;
using AgroSolutions.Ingestao.WebApi.Controllers;
using AgroSolutions.Ingestao.WebApi.Domain;
using AgroSolutions.Ingestao.WebApi.Infrastructure.Mensageria;
using AgroSolutions.Ingestao.WebApi.Infrastructure.Observability;
using AgroSolutions.Ingestao.WebApi.Infrastructure.Repositorios;
using AgroSolutions.Ingestao.WebApi.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AgroSolutions.Ingestao.WebApi.Tests;

public class LeiturasSensoresControllerTests
{
    private const string OrigemTeste = "teste";
    private readonly Mock<ILeituraSensorRepositorio> _repositorioMock;
    private readonly Mock<IEventoPublisher> _publisherMock;
    private readonly Mock<IPropriedadesService> _propriedadesServiceMock;
    private readonly LeiturasSensoresController _controller;

    public LeiturasSensoresControllerTests()
    {
        _repositorioMock = new Mock<ILeituraSensorRepositorio>();
        _publisherMock = new Mock<IEventoPublisher>();
        _propriedadesServiceMock = new Mock<IPropriedadesService>();
        
        var logger = NullLogger<LeiturasSensoresController>.Instance;

        // Metric setup
        var meterFactoryMock = new Mock<IMeterFactory>();
        // Using a real Meter for simplicity as mocking final classes/methods is hard, 
        // but we just need it not to crash.
        // However, Meter constructor is protected or internal often? 
        // Actually Meter public constructor exists in .NET 8.
        // Let's rely on a simple factory implementation or minimal mock setup.
        // If IMeterFactory is mocked to return a dummy Meter, CreateCounter works.
        
        // For unit testing, we can just pass a dummy implementation of IngestaoMetrics if possible, 
        // but it's a concrete class. 
        // Let's create a real instance with a mocked factory that returns a real Meter.
        meterFactoryMock.Setup(x => x.Create(It.IsAny<MeterOptions>())).Returns(new Meter("TestMeter"));
        // Note: IMeterFactory.Create overload might vary.

        // Simpler approach: If we don't assert on metrics, we just need it to run.
        // But IngestaoMetrics calls .Create and .CreateCounter in constructor.
        
        // Let's try passing null for metrics first? No, constructor uses it.
        // We'll use a test-friendly approach.
        
        /* 
           Since .NET 8, we can just use the ServiceCollection to get a real IMeterFactory 
           or just mock it to return a new Meter("name").
        */
        
        var meter = new Meter("AgroSolutions.Ingestao.Tests");
        meterFactoryMock.Setup(m => m.Create(It.IsAny<MeterOptions>())).Returns(meter);
        var metrics = new IngestaoMetrics(meterFactoryMock.Object);

        // Setup successful validation by default to keep existing tests passing
        _propriedadesServiceMock.Setup(x => x.ValidateTalhaoOwnershipAsync(It.IsAny<Guid>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        _controller = new LeiturasSensoresController(
            _repositorioMock.Object,
            _publisherMock.Object,
            logger,
            metrics,
            _propriedadesServiceMock.Object);
        
        // Mock ControllerContext for User/Auth if needed, 
        // but existing tests might not use [Authorize] attributes validation 
        // unless integration tests. 
        // Use default if tests invoke method directly.
        // The controller checks token in header.
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Authorization"] = "Bearer token-mock";
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
    }

    [Fact]
    public async Task CriarAsync_DeveRetornarBadRequest_QuandoNenhumaMetricaForInformada()
    {
        // Arrange
        var request = new CriarLeituraSensorRequest
        {
            IdPropriedade = Guid.NewGuid(),
            IdTalhao = Guid.NewGuid(),
            Origem = OrigemTeste,
            DataHoraCapturaUtc = DateTime.UtcNow,
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
            Origem = OrigemTeste,
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

    [Fact]
    public async Task CriarAsync_DeveDefinirDataHoraComoUtc()
    {
        // Arrange
        var request = new CriarLeituraSensorRequest
        {
            IdPropriedade = Guid.NewGuid(),
            IdTalhao = Guid.NewGuid(),
            Origem = OrigemTeste,
            DataHoraCapturaUtc = new DateTime(2026, 2, 5, 10, 0, 0, DateTimeKind.Local),
            Metricas = new MetricasSensorRequest
            {
                UmidadeSoloPercentual = 55
            }
        };

        LeituraSensor? leituraInserida = null;

        _repositorioMock
            .Setup(x => x.InserirAsync(It.IsAny<LeituraSensor>(), It.IsAny<CancellationToken>()))
            .Callback<LeituraSensor, CancellationToken>((leitura, _) => leituraInserida = leitura)
            .ReturnsAsync(10);

        // Act
        await _controller.CriarAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(leituraInserida);
        Assert.Equal(DateTimeKind.Utc, leituraInserida!.DataHoraCapturaUtc.Kind);
    }

    [Fact]
    public async Task CriarAsync_DeveLancarExcecao_QuandoRepositorioFalhar()
    {
        // Arrange
        var request = new CriarLeituraSensorRequest
        {
            IdPropriedade = Guid.NewGuid(),
            IdTalhao = Guid.NewGuid(),
            Origem = OrigemTeste,
            DataHoraCapturaUtc = DateTime.UtcNow,
            Metricas = new MetricasSensorRequest
            {
                TemperaturaCelsius = 21
            }
        };

        _repositorioMock
            .Setup(x => x.InserirAsync(It.IsAny<LeituraSensor>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Falha no repositório"));

        // Act + Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _controller.CriarAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task ConsultarAsync_DevePassarAgruparMinutosParaRepositorio()
    {
        // Arrange
        var idTalhao = Guid.NewGuid();
        var de = new DateTime(2026, 2, 5, 0, 0, 0, DateTimeKind.Local);
        var ate = de.AddHours(1);
        const int agruparMinutos = 15;

        _repositorioMock
            .Setup(x => x.ConsultarAsync(idTalhao, It.IsAny<DateTime>(), It.IsAny<DateTime>(), agruparMinutos, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<LeituraSensor>());

        // Act
        var result = await _controller.ConsultarAsync(idTalhao, de, ate, agruparMinutos, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result.Result);
        _repositorioMock.Verify(x => x.ConsultarAsync(
            idTalhao,
            It.Is<DateTime>(d => d.Kind == DateTimeKind.Utc),
            It.Is<DateTime>(d => d.Kind == DateTimeKind.Utc),
            agruparMinutos,
            It.IsAny<CancellationToken>()), Times.Once);
    }
}

