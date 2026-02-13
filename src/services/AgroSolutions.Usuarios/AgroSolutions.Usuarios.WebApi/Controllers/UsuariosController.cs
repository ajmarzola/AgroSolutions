using AgroSolutions.Usuarios.WebApi.Data;
using AgroSolutions.Usuarios.WebApi.DTOs;
using AgroSolutions.Usuarios.WebApi.Entity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;

namespace AgroSolutions.Usuarios.WebApi.Controllers
{
    [ApiController]
    [Route("api/usuarios")]
    public class UsuariosController : ControllerBase
    {
        private readonly AgroDbContext _context;
        private readonly IConfiguration _config;

        public UsuariosController(AgroDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto login)
        {
            var user = await _context.Usuarios.Include(u => u.Tipo)
                .FirstOrDefaultAsync(u => u.Email == login.Email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(login.Password, user.Senha))
            {
                return Unauthorized("E-mail ou senha incorretos.");
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            string keyString = _config["Jwt:Key"];
            if (string.IsNullOrEmpty(keyString))
            {
                throw new InvalidOperationException("JWT Key não configurada.");
            }
            var key = Encoding.ASCII.GetBytes(keyString);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] {
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Tipo?.Descricao ?? "Produtor"),
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
                }),
                Expires = DateTime.UtcNow.AddHours(8),
                Audience = _config["Jwt:Audience"],
                Issuer = _config["Jwt:Issuer"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return Ok(new { Token = tokenHandler.WriteToken(token) });
        }

        [HttpPost("registrar")]
        public async Task<IActionResult> Registrar([FromBody] UsuarioRegistroDto usuarioDto)
        {
            if (await _context.Usuarios.AnyAsync(u => u.Email == usuarioDto.Email))
            {
                return BadRequest("E-mail já cadastrado.");
            }

            var usuario = new Usuario
            {
                Email = usuarioDto.Email,
                Senha = BCrypt.Net.BCrypt.HashPassword(usuarioDto.Senha),
                TipoId = usuarioDto.TipoId
            };

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();
            return Ok(new { mensagem = "Usuário criado com sucesso!" });
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ListarTodos()
        {
            var usuarios = await _context.Usuarios
                .Include(u => u.Tipo)
                .Select(u => new UsuarioResponseDto 
                { 
                    Id = u.Id, 
                    Email = u.Email, 
                    TipoDescricao = u.Tipo != null ? u.Tipo.Descricao : "N/A"
                })
                .ToListAsync();

            return Ok(usuarios);
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Remover(int id)
        {
            var user = await _context.Usuarios.FindAsync(id);
            if (user == null) return NotFound();
            _context.Usuarios.Remove(user);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}