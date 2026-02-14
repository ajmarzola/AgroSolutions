using AgroSolutions.Propriedades.WebApi.Data;
using AgroSolutions.Propriedades.WebApi.DTOs;
using AgroSolutions.Propriedades.WebApi.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AgroSolutions.Propriedades.WebApi.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class PropriedadesController : ControllerBase
{
    private readonly PropriedadesDbContext _context;

    public PropriedadesController(PropriedadesDbContext context)
    {
        _context = context;
    }

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
