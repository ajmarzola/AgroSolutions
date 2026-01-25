using AgroSolutions.Ingestao.WebApi.Contracts.Requests;
using AgroSolutions.Ingestao.WebApi.Contracts.Responses;
using AgroSolutions.Ingestao.WebApi.Domain;
using AgroSolutions.Ingestao.WebApi.Infrastructure.Mensageria;
using AgroSolutions.Ingestao.WebApi.Infrastructure.Repositorios;
using Microsoft.AspNetCore.Mvc;

namespace AgroSolutions.Ingestao.WebApi.Controllers;

[ApiController]
[Route("api/v1/leituras-sensores")]
public sealed class LeiturasSensoresController : ControllerBase
{
    private readonly ILeituraSensorRepositorio _repositorio;
    private readonly IEventoPublisher _publisher;
    private readonly ILogger<LeiturasSensoresController> _logger;

    public LeiturasSensoresController(
        ILeituraSensorRepositorio repositorio,
        IEventoPublisher publisher,
        ILogger<LeiturasSensoresController> logger)
    {
        _repositorio = repositorio;
        _publisher = publisher;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CriarAsync([FromBody] CriarLeituraSensorRequest request, CancellationToken ct)
    {
        // Validação extra: ao menos uma métrica preenchida
        if (request.Metricas.UmidadeSoloPercentual is null
            && request.Metricas.TemperaturaCelsius is null
            && request.Metricas.PrecipitacaoMilimetros is null)
        {
            ModelState.AddModelError("metricas", "Informe ao menos uma métrica (umidade/temperatura/precipitação).");
            return ValidationProblem(ModelState);
        }

        var leitura = new LeituraSensor
        {
            IdPropriedade = request.IdPropriedade,
            IdTalhao = request.IdTalhao,
            Origem = request.Origem,
            DataHoraCapturaUtc = DateTime.SpecifyKind(request.DataHoraCapturaUtc, DateTimeKind.Utc),
            UmidadeSoloPercentual = request.Metricas.UmidadeSoloPercentual,
            TemperaturaCelsius = request.Metricas.TemperaturaCelsius,
            PrecipitacaoMilimetros = request.Metricas.PrecipitacaoMilimetros,
            IdDispositivo = request.Meta?.IdDispositivo,
            CorrelationId = request.Meta?.CorrelationId
        };

        var id = await _repositorio.InserirAsync(leitura, ct);
        leitura.Id = id;

        _logger.LogInformation("Leitura recebida. Id={Id} Talhao={IdTalhao} CapturaUtc={DataHoraCapturaUtc} Origem={Origem}",
            leitura.Id, leitura.IdTalhao, leitura.DataHoraCapturaUtc, leitura.Origem);

        // Publica evento para o serviço de Análise (assíncrono)
        await _publisher.PublicarLeituraRecebidaAsync(leitura, ct);

        return CreatedAtAction(nameof(ConsultarAsync), new
        {
            idTalhao = leitura.IdTalhao,
            deUtc = leitura.DataHoraCapturaUtc.AddMinutes(-1).ToString("O"),
            ateUtc = leitura.DataHoraCapturaUtc.AddMinutes(1).ToString("O")
        }, new { id = leitura.Id });
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<LeituraSensorResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<LeituraSensorResponse>>> ConsultarAsync(
        [FromQuery] Guid idTalhao,
        [FromQuery] DateTime deUtc,
        [FromQuery] DateTime ateUtc,
        [FromQuery] int? agruparMinutos,
        CancellationToken ct)
    {
        if (idTalhao == Guid.Empty)
            return BadRequest("idTalhao é obrigatório.");

        if (deUtc == default || ateUtc == default || ateUtc <= deUtc)
            return BadRequest("Intervalo inválido. Informe deUtc e ateUtc (ateUtc > deUtc).");

        var rows = await _repositorio.ConsultarAsync(idTalhao, DateTime.SpecifyKind(deUtc, DateTimeKind.Utc),
            DateTime.SpecifyKind(ateUtc, DateTimeKind.Utc), agruparMinutos, ct);

        var result = rows.Select(x => new LeituraSensorResponse
        {
            Id = x.Id,
            IdPropriedade = x.IdPropriedade,
            IdTalhao = x.IdTalhao,
            Origem = x.Origem,
            DataHoraCapturaUtc = x.DataHoraCapturaUtc,
            Metricas = new()
            {
                UmidadeSoloPercentual = x.UmidadeSoloPercentual,
                TemperaturaCelsius = x.TemperaturaCelsius,
                PrecipitacaoMilimetros = x.PrecipitacaoMilimetros
            },
            Meta = new()
            {
                IdDispositivo = x.IdDispositivo,
                CorrelationId = x.CorrelationId
            }
        }).ToList();

        return Ok(result);
    }
}
