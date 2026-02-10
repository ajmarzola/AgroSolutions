using AgroSolutions.Usuarios.WebApi.Controllers;
using AgroSolutions.Usuarios.WebApi.Data;
using AgroSolutions.Usuarios.WebApi.DTOs;
using AgroSolutions.Usuarios.WebApi.Entity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace AgroSolutions.Usuarios.WebApi.Tests;

public class UsuariosControllerTests
{
    private readonly AgroDbContext _context;
    private readonly Mock<IConfiguration> _configMock;
    private readonly UsuariosController _controller;

    public UsuariosControllerTests()
    {
        var options = new DbContextOptionsBuilder<AgroDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Unique DB per test
            .Options;
        
        _context = new AgroDbContext(options);
        _configMock = new Mock<IConfiguration>();
        _configMock.Setup(c => c["Jwt:Key"]).Returns("MinhaChaveSuperSecretaDeTeste123456!");

        _controller = new UsuariosController(_context, _configMock.Object);
    }

    [Fact]
    public async Task Registrar_EmailDuplicado_RetornaBadRequest()
    {
        // Arrange
        var email = "teste@exemplo.com";
        _context.Usuarios.Add(new Usuario { Email = email, Senha = "hash", TipoId = 1 });
        await _context.SaveChangesAsync();

        var dto = new RegistroUsuarioDto { Email = email, Senha = "123", TipoId = 1 };

        // Act
        var result = await _controller.Registrar(dto);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("E-mail já cadastrado.", badRequest.Value);
    }

    [Fact]
    public async Task Registrar_NovoUsuario_RetornaOk()
    {
        // Arrange
        var dto = new RegistroUsuarioDto { Email = "novo@exemplo.com", Senha = "123", TipoId = 1 };

        // Act
        var result = await _controller.Registrar(dto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        
        var user = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == dto.Email);
        Assert.NotNull(user);
    }

    [Fact]
    public async Task Login_CredenciaisValidas_RetornaToken()
    {
        // Add Tipo to DB
        _context.TiposUsuarios.Add(new TipoUsuario { Id = 1, Descricao = "Produtor" });
        await _context.SaveChangesAsync();

        // Arrange
        var email = "login_sucesso@exemplo.com";
        var password = "SenhaForte123!";
        
        // Create user via controller to ensure hash consistency
        await _controller.Registrar(new RegistroUsuarioDto { Email = email, Senha = password, TipoId = 1 });

        var dto = new LoginRequestDto { Email = email, Password = password };

        // Act
        var result = await _controller.Login(dto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var val = okResult.Value;
        var tokenProp = val?.GetType().GetProperty("Token")?.GetValue(val, null);
        Assert.NotNull(tokenProp);
    }

    [Fact]
    public async Task Login_SenhaInvalida_RetornaUnauthorized()
    {
        // Arrange
        var password = "password123";
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);
        var email = "login_errado@exemplo.com";
        
        _context.Usuarios.Add(new Usuario { Email = email, Senha = hashedPassword, TipoId = 1 });
        await _context.SaveChangesAsync();

        var dto = new LoginRequestDto { Email = email, Password = "wrong_password" };

        // Act
        var result = await _controller.Login(dto);

        // Assert
        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
    }
}
