using AgroSolutions.Propriedades.WebApi.Data;
using AgroSolutions.Propriedades.WebApi.DTOs;
using AgroSolutions.Propriedades.WebApi.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AgroSolutions.Propriedades.WebApi.Controllers;

/// <summary>
/// Controladora para gestão de propriedades rurais e seus talhões.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class PropriedadesController : ControllerBase
{
    private readonly PropriedadesDbContext _context;

    public PropriedadesController(PropriedadesDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Lista as propriedades do usuário autenticado.
    /// </summary>
    /// <returns>Lista de propriedades cadastradas.</returns>
    /// <response code="200">Retorna a lista de propriedades.</response>
    /// <response code="401">Não autorizado.</response>
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IEnumerable<PropriedadeDto>>> GetPropriedades()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var propriedades = await _context.Propriedades
            .Where(p => p.OwnerUserId == userId)
            .Select(p => new PropriedadeDto(p.Id, p.Nome, p.Localizacao))
            .ToListAsync();
        return Ok(propriedades);
    }

    /// <summary>
    /// Cadastra uma nova propriedade.
    /// </summary>
    /// <param name="dto">Dados da nova propriedade (nome e localização).</param>
    /// <returns>Dados da propriedade criada.</returns>
    /// <response code="201">Propriedade criada com sucesso.</response>
    /// <response code="401">Não autorizado.</response>
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<PropriedadeDto>> CreatePropriedade(CreatePropriedadeDto dto)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var propriedade = new Propriedade
        {
            Id = Guid.NewGuid(),
            Nome = dto.Nome,
            Localizacao = dto.Localizacao,
            OwnerUserId = userId
        };

        _context.Propriedades.Add(propriedade);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetPropriedades), new { id = propriedade.Id }, 
            new PropriedadeDto(propriedade.Id, propriedade.Nome, propriedade.Localizacao));
    }

    /// <summary>
    /// Lista os talhões de uma propriedade específica.
    /// </summary>
    /// <param name="id">ID da propriedade.</param>
    /// <returns>Lista de talhões da propriedade.</returns>
    /// <response code="200">Retorna a lista de talhões.</response>
    /// <response code="404">Propriedade não encontrada.</response>
    /// <response code="401">Não autorizado.</response>
    [HttpGet("{id}/talhoes")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<TalhaoDto>>> GetTalhoes(Guid id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var exists = await _context.Propriedades.AnyAsync(p => p.Id == id && p.OwnerUserId == userId);
        if (!exists)
        {
            return NotFound("Propriedade não encontrada");
        }

        var talhoes = await _context.Talhoes
            .Where(t => t.PropriedadeId == id)
            .Select(t => new TalhaoDto(t.Id, t.PropriedadeId, t.Nome, t.Cultura, t.Area))
            .ToListAsync();
        
        return Ok(talhoes);
    }

    /// <summary>
    /// Cria um novo talhão em uma propriedade.
    /// </summary>
    /// <param name="id">ID da propriedade onde o talhão será criado.</param>
    /// <param name="dto">Dados do novo talhão.</param>
    /// <returns>Dados do talhão criado.</returns>
    /// <response code="201">Talhão criado com sucesso.</response>
    /// <response code="404">Propriedade não encontrada.</response>
    /// <response code="401">Não autorizado.</response>
    [HttpPost("{id}/talhoes")]
    [Authorize]
    public async Task<ActionResult<TalhaoDto>> CreateTalhao(Guid id, CreateTalhaoDto dto)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var propriedade = await _context.Propriedades.FindAsync(id);
        if (propriedade == null || propriedade.OwnerUserId != userId)
        {
            return NotFound("Propriedade não encontrada");
        }

        var talhao = new Talhao
        {
            Id = Guid.NewGuid(),
            PropriedadeId = id,
            Nome = dto.Nome,
            Cultura = dto.Cultura,
            Area = dto.Area
        };

        _context.Talhoes.Add(talhao);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetTalhoes), new { id = id },
            new TalhaoDto(talhao.Id, talhao.PropriedadeId, talhao.Nome, talhao.Cultura, talhao.Area));
    }

    /// <summary>
    /// Obtém detalhes de um talhão específico.
    /// </summary>
    /// <param name="id">ID do talhão.</param>
    /// <returns>Detalhes do talhão.</returns>
    /// <response code="200">Retorna os detalhes do talhão.</response>
    /// <response code="404">Talhão não encontrado.</response>
    /// <response code="403">Você não tem permissão para acessar este talhão.</response>
    /// <response code="401">Não autorizado.</response>
    [HttpGet("talhoes/{id}")]
    [Authorize]
    public async Task<ActionResult<TalhaoDto>> GetTalhao(Guid id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var talhao = await _context.Talhoes
            .Join(_context.Propriedades,
                t => t.PropriedadeId,
                p => p.Id,
                (t, p) => new { Talhao = t, Propriedade = p })
            .Where(x => x.Talhao.Id == id)
            .FirstOrDefaultAsync();

        if (talhao == null)
        {
            return NotFound("Talhão não encontrado.");
        }

        if (talhao.Propriedade.OwnerUserId != userId)
        {
            return StatusCode(StatusCodes.Status403Forbidden, "Você não tem permissão para acessar este talhão.");
        }

        return Ok(new TalhaoDto(talhao.Talhao.Id, talhao.Talhao.PropriedadeId, talhao.Talhao.Nome, talhao.Talhao.Cultura, talhao.Talhao.Area));
    }
}
