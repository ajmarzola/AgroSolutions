using AgroSolutions.Analise.WebApi.Domain;
using AgroSolutions.Analise.WebApi.Infrastructure.Repositorios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroSolutions.Analise.WebApi.Controllers;

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

    [HttpGet("leituras")]
    public async Task<ActionResult<IEnumerable<Leitura>>> ObterLeituras([FromQuery] Guid? idTalhao, [FromQuery] int top = 100)
    {
        var result = await _repositorio.ListarLeiturasAsync(idTalhao, top);
        return Ok(result);
    }

    [HttpGet("alertas")]
    public async Task<ActionResult<IEnumerable<Alerta>>> ObterAlertas([FromQuery] Guid? idTalhao, [FromQuery] int top = 100)
    {
        var result = await _repositorio.ListarAlertasAsync(idTalhao, top);
        return Ok(result);
    }
}
