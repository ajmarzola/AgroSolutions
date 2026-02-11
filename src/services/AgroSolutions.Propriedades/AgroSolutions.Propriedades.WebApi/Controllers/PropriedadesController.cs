using AgroSolutions.Propriedades.WebApi.Data;
using AgroSolutions.Propriedades.WebApi.DTOs;
using AgroSolutions.Propriedades.WebApi.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
    public async Task<ActionResult<IEnumerable<PropriedadeDto>>> GetPropriedades()
    {
        var propriedades = await _context.Propriedades
            .Select(p => new PropriedadeDto(p.Id, p.Nome, p.Localizacao))
            .ToListAsync();
        return Ok(propriedades);
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<PropriedadeDto>> CreatePropriedade(CreatePropriedadeDto dto)
    {
        var propriedade = new Propriedade
        {
            Id = Guid.NewGuid(),
            Nome = dto.Nome,
            Localizacao = dto.Localizacao
        };

        _context.Propriedades.Add(propriedade);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetPropriedades), new { id = propriedade.Id }, 
            new PropriedadeDto(propriedade.Id, propriedade.Nome, propriedade.Localizacao));
    }

    [HttpGet("{id}/talhoes")]
    public async Task<ActionResult<IEnumerable<TalhaoDto>>> GetTalhoes(Guid id)
    {
        var exists = await _context.Propriedades.AnyAsync(p => p.Id == id);
        if (!exists) return NotFound("Propriedade não encontrada");

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
        var propriedade = await _context.Propriedades.FindAsync(id);
        if (propriedade == null) return NotFound("Propriedade não encontrada");

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
}
