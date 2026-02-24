using AgroSolutions.Ingestao.WebApi.Contracts.Requests;
using AgroSolutions.Ingestao.WebApi.Contracts.Responses;
using AgroSolutions.Ingestao.WebApi.Domain;
using AgroSolutions.Ingestao.WebApi.Infrastructure.Mensageria;
using AgroSolutions.Ingestao.WebApi.Infrastructure.Observability;
using AgroSolutions.Ingestao.WebApi.Infrastructure.Repositorios;
using AgroSolutions.Ingestao.WebApi.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace AgroSolutions.Ingestao.WebApi.Controllers;

/// <summary>
/// Controladora responsável por receber e processar leituras de sensores IoT.
/// </summary>
[ApiController]
[Route("api/v1/leituras-sensores")]
public sealed class LeiturasSensoresController : ControllerBase
{
    private readonly ILeituraSensorRepositorio _repositorio;
    private readonly IEventoPublisher _publisher;
    private readonly ILogger<LeiturasSensoresController> _logger;
    private readonly IngestaoMetrics _metrics;
    private readonly IPropriedadesService _propriedadesService;

    public LeiturasSensoresController(
        ILeituraSensorRepositorio repositorio,
        IEventoPublisher publisher,
        ILogger<LeiturasSensoresController> logger,
        IngestaoMetrics metrics,
        IPropriedadesService propriedadesService)
    {
        _repositorio = repositorio;
        _publisher = publisher;
        _logger = logger;
        _metrics = metrics;
        _propriedadesService = propriedadesService;
    }

    /// <summary>
    /// Recebe uma nova leitura de sensor.
    /// </summary>
    /// <remarks>
    /// Valida se o usuário tem permissão para enviar dados para o talhão especificado e publica o evento para processamento.
    /// </remarks>
    /// <param name="request">Dados da leitura do sensor (talhão, métricas, etc).</param>
    /// <param name="ct">Token de cancelamento.</param>
    /// <returns>ID da leitura criada.</returns>
    /// <response code="201">Leitura criada com sucesso.</response>
    /// <response code="400">Dados inválidos (ex: nenhuma métrica informada).</response>
    /// <response code="403">Sem permissão para o talhão informado.</response>
    /// <response code="401">Não autorizado.</response>
    [Authorize]
    [HttpPost]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CriarAsync([FromBody] CriarLeituraSensorRequest request, CancellationToken ct)
    {
        // Cross-Tenant Validation
        var token = Request.Headers.Authorization.ToString();
        if (string.IsNullOrEmpty(token))
        {
            return Unauthorized();
        }

        var isOwner = await _propriedadesService.ValidateTalhaoOwnershipAsync(request.IdTalhao, token);
        if (!isOwner)
        {
            return Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Forbidden",
                detail: "Você não tem permissão para enviar leituras para este talhão."
            );
        }

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

        // Métricas de negócio
        _metrics.LeiturasTotal.Add(1,
            new KeyValuePair<string, object?>("propriedade_id", leitura.IdPropriedade),
            new KeyValuePair<string, object?>("talhao_id", leitura.IdTalhao));

        var id = await _repositorio.InserirAsync(leitura, ct);
        leitura.Id = id;

        _logger.LogInformation("Leitura recebida. Id={Id} Talhao={IdTalhao} CapturaUtc={DataHoraCapturaUtc} Origem={Origem}",
            leitura.Id, leitura.IdTalhao, leitura.DataHoraCapturaUtc, leitura.Origem);

        // Publica evento para o serviço de Análise (assíncrono)
        await _publisher.PublicarLeituraRecebidaAsync(leitura, ct);

        // Retorna 201 Created com o ID gerado
        return StatusCode(StatusCodes.Status201Created, new { id = leitura.Id });
    }

    /// <summary>
    /// Consulta o histórico de leituras de um talhão.
    /// </summary>
    /// <param name="idTalhao">ID do talhão para consulta.</param>
    /// <param name="deUtc">Data inicial (UTC).</param>
    /// <param name="ateUtc">Data final (UTC).</param>
    /// <param name="agruparMinutos">Opcional: Agrupar leituras em janelas de tempo (minutos).</param>
    /// <param name="ct">Token de cancelamento.</param>
    /// <returns>Lista de leituras encontradas.</returns>
    /// <response code="200">Retorna a lista de leituras.</response>
    /// <response code="400">Parâmetros inválidos (datas ou ID).</response>
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
        {
            return BadRequest("idTalhao é obrigatório.");
        }

        if (deUtc == default || ateUtc == default || ateUtc <= deUtc)
        {
            return BadRequest("Intervalo inválido. Informe deUtc e ateUtc (ateUtc > deUtc).");
        }

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
