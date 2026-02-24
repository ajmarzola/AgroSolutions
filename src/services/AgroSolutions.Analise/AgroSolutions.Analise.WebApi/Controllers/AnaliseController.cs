using AgroSolutions.Analise.WebApi.Domain;
using AgroSolutions.Analise.WebApi.Infrastructure.Repositorios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroSolutions.Analise.WebApi.Controllers;

/// <summary>
/// Controladora responsável pela exposição de dados analíticos e alertas.
/// </summary>
[Authorize]
[ApiController]
[Route("api/v1/analise")]
public class AnaliseController : ControllerBase
{
    private readonly IAnaliseRepositorio _repositorio;

    public AnaliseController(IAnaliseRepositorio repositorio)
    {
        _repositorio = repositorio;
    }

    /// <summary>
    /// Consulta o histórico de leituras armazenadas para análise.
    /// </summary>
    /// <param name="idTalhao">Opcional: Filtrar por ID do talhão.</param>
    /// <param name="top">Número máximo de registros a retornar (padrão: 100).</param>
    /// <returns>Lista de leituras.</returns>
    /// <response code="200">Retorna a lista de leituras.</response>
    /// <response code="401">Não autorizado.</response>
    [HttpGet("leituras")]
    public async Task<ActionResult<IEnumerable<Leitura>>> ObterLeituras([FromQuery] Guid? idTalhao, [FromQuery] int top = 100)
    {
        var result = await _repositorio.ListarLeiturasAsync(idTalhao, top);
        return Ok(result);
    }

    /// <summary>
    /// Consulta os alertas gerados pelo motor de regras.
    /// </summary>
    /// <param name="idTalhao">Opcional: Filtrar por ID do talhão.</param>
    /// <param name="top">Número máximo de alertas a retornar (padrão: 100).</param>
    /// <returns>Lista de alertas.</returns>
    /// <response code="200">Retorna a lista de alertas.</response>
    /// <response code="401">Não autorizado.</response>
    [HttpGet("alertas")]
    public async Task<ActionResult<IEnumerable<Alerta>>> ObterAlertas([FromQuery] Guid? idTalhao, [FromQuery] int top = 100)
    {
        var result = await _repositorio.ListarAlertasAsync(idTalhao, top);
        return Ok(result);
    }
}
