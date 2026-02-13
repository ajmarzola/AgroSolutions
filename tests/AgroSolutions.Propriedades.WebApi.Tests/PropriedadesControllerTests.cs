using System.Security.Claims;
using AgroSolutions.Propriedades.WebApi.Controllers;
using AgroSolutions.Propriedades.WebApi.Data;
using AgroSolutions.Propriedades.WebApi.DTOs;
using AgroSolutions.Propriedades.WebApi.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AgroSolutions.Propriedades.WebApi.Tests;

public class PropriedadesControllerTests
{
    private DbContextOptions<PropriedadesDbContext> CreateNewContextOptions()
    {
        return new DbContextOptionsBuilder<PropriedadesDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    private PropriedadesController CreateController(PropriedadesDbContext context, string? userId = "test-user-id")
    {
        var controller = new PropriedadesController(context);

        if (userId != null)
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId)
            }, "mock"));

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }
        else
        {
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
        }

        return controller;
    }

    [Fact]
    public async Task GetPropriedades_ReturnsEmptyList_WhenNoPropertiesExistForUser()
    {
        // Arrange
        using var context = new PropriedadesDbContext(CreateNewContextOptions());
        var controller = CreateController(context, "user1");

        // Act
        var result = await controller.GetPropriedades();

        // Assert
        var actionResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedList = Assert.IsAssignableFrom<IEnumerable<PropriedadeDto>>(actionResult.Value);
        Assert.Empty(returnedList);
    }

    [Fact]
    public async Task GetPropriedades_ReturnsProperties_ForSpecificUserOnly()
    {
        // Arrange
        using var context = new PropriedadesDbContext(CreateNewContextOptions());
        var userId = "user1";
        var otherUserId = "user2";

        context.Propriedades.Add(new Propriedade { Id = Guid.NewGuid(), Nome = "Fazenda 1", Localizacao = "Loc 1", OwnerUserId = userId });
        context.Propriedades.Add(new Propriedade { Id = Guid.NewGuid(), Nome = "Fazenda 2", Localizacao = "Loc 2", OwnerUserId = userId });
        context.Propriedades.Add(new Propriedade { Id = Guid.NewGuid(), Nome = "Fazenda Other", Localizacao = "Loc Other", OwnerUserId = otherUserId });
        await context.SaveChangesAsync();

        var controller = CreateController(context, userId);

        // Act
        var result = await controller.GetPropriedades();

        // Assert
        var actionResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedList = Assert.IsAssignableFrom<IEnumerable<PropriedadeDto>>(actionResult.Value);
        Assert.Equal(2, returnedList.Count());
        Assert.All(returnedList, p => Assert.NotEqual("Fazenda Other", p.Nome));
    }

    [Fact]
    public async Task CreatePropriedade_ReturnsUnauthorized_WhenUserNotAuthenticated()
    {
        // Arrange
        using var context = new PropriedadesDbContext(CreateNewContextOptions());
        // Simulating no user claim
        var controller = CreateController(context, userId: null); 
        
        var dto = new CreatePropriedadeDto("Fazenda Nova", "Loc Nova");

        // Act
        var result = await controller.CreatePropriedade(dto);

        // Assert
        Assert.IsType<UnauthorizedResult>(result.Result);
    }

    [Fact]
    public async Task CreatePropriedade_CreatesAndReturnsPropriedade()
    {
        // Arrange
        using var context = new PropriedadesDbContext(CreateNewContextOptions());
        var userId = "user-new";
        var controller = CreateController(context, userId);
        var dto = new CreatePropriedadeDto("Fazenda Criada", "Loc Criada");

        // Act
        var result = await controller.CreatePropriedade(dto);

        // Assert
        var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var returnDto = Assert.IsType<PropriedadeDto>(createdAtActionResult.Value);
        
        Assert.Equal(dto.Nome, returnDto.Nome);
        Assert.Equal(dto.Localizacao, returnDto.Localizacao);
        Assert.NotEqual(Guid.Empty, returnDto.Id);

        // Verify database
        var dbProp = await context.Propriedades.FirstOrDefaultAsync(p => p.Id == returnDto.Id);
        Assert.NotNull(dbProp);
        Assert.Equal(userId, dbProp.OwnerUserId);
    }

    [Fact]
    public async Task GetTalhoes_ReturnsNotFound_WhenPropriedadeDoesNotExist()
    {
        // Arrange
        using var context = new PropriedadesDbContext(CreateNewContextOptions());
        var controller = CreateController(context, "user1");
        
        // Act
        var result = await controller.GetTalhoes(Guid.NewGuid());

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetTalhoes_ReturnsNotFound_WhenPropriedadeBelongsToAnotherUser()
    {
        // Arrange
        using var context = new PropriedadesDbContext(CreateNewContextOptions());
        var ownerId = "owner";
        var otherId = "intruder";
        
        var propId = Guid.NewGuid();
        context.Propriedades.Add(new Propriedade { Id = propId, Nome = "Fazenda", Localizacao = "Loc", OwnerUserId = ownerId });
        await context.SaveChangesAsync();

        var controller = CreateController(context, otherId);

        // Act
        var result = await controller.GetTalhoes(propId);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetTalhoes_ReturnsTalhoes_ForPropriedade()
    {
        // Arrange
        using var context = new PropriedadesDbContext(CreateNewContextOptions());
        var userId = "user1";
        var propId = Guid.NewGuid();
        
        context.Propriedades.Add(new Propriedade { Id = propId, Nome = "Fazenda", Localizacao = "Loc", OwnerUserId = userId });
        context.Talhoes.Add(new Talhao { Id = Guid.NewGuid(), PropriedadeId = propId, Nome = "Talhao 1", Cultura = "Soja", Area = 100 });
        context.Talhoes.Add(new Talhao { Id = Guid.NewGuid(), PropriedadeId = propId, Nome = "Talhao 2", Cultura = "Milho", Area = 200 });
        
        // Another property's talhao
        var otherPropId = Guid.NewGuid();
        context.Propriedades.Add(new Propriedade { Id = otherPropId, Nome = "Fazenda 2", Localizacao = "Loc 2", OwnerUserId = userId });
        context.Talhoes.Add(new Talhao { Id = Guid.NewGuid(), PropriedadeId = otherPropId, Nome = "Talhao 3", Cultura = "Trigo", Area = 50 });

        await context.SaveChangesAsync();

        var controller = CreateController(context, userId);

        // Act
        var result = await controller.GetTalhoes(propId);

        // Assert
        var actionResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedList = Assert.IsAssignableFrom<IEnumerable<TalhaoDto>>(actionResult.Value);
        Assert.Equal(2, returnedList.Count());
    }

    [Fact]
    public async Task CreateTalhao_ReturnsNotFound_WhenPropriedadeDoesNotExist()
    {
        // Arrange
        using var context = new PropriedadesDbContext(CreateNewContextOptions());
        var controller = CreateController(context, "user1");
        var dto = new CreateTalhaoDto("Talhao 1", "Soja", 10.5m); // Adjusted constructor arguments

        // Act
        var result = await controller.CreateTalhao(Guid.NewGuid(), dto);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateTalhao_ReturnsNotFound_WhenPropriedadeBelongsToAnotherUser()
    {
        // Arrange
        using var context = new PropriedadesDbContext(CreateNewContextOptions());
        var ownerId = "owner";
        var intruderId = "intruder";
        var propId = Guid.NewGuid();

        context.Propriedades.Add(new Propriedade { Id = propId, Nome = "Fazenda", Localizacao = "Loc", OwnerUserId = ownerId });
        await context.SaveChangesAsync();

        var controller = CreateController(context, intruderId);
        var dto = new CreateTalhaoDto("Talhao 1", "Soja", 10.5m);

        // Act
        var result = await controller.CreateTalhao(propId, dto);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateTalhao_CreatesAndReturnsTalhao()
    {
        // Arrange
        using var context = new PropriedadesDbContext(CreateNewContextOptions());
        var userId = "user1";
        var propId = Guid.NewGuid();

        context.Propriedades.Add(new Propriedade { Id = propId, Nome = "Fazenda", Localizacao = "Loc", OwnerUserId = userId });
        await context.SaveChangesAsync();

        var controller = CreateController(context, userId);
        var dto = new CreateTalhaoDto("Talhao Novo", "Café", 50.0m);

        // Act
        var result = await controller.CreateTalhao(propId, dto);

        // Assert
        var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var returnDto = Assert.IsType<TalhaoDto>(createdAtActionResult.Value);

        Assert.Equal(dto.Nome, returnDto.Nome);
        Assert.Equal(dto.Cultura, returnDto.Cultura);
        Assert.Equal(dto.Area, returnDto.Area);
        Assert.Equal(propId, returnDto.PropriedadeId);
        Assert.NotEqual(Guid.Empty, returnDto.Id);

        // Verify DB
        var dbTalhao = await context.Talhoes.FirstOrDefaultAsync(t => t.Id == returnDto.Id);
        Assert.NotNull(dbTalhao);
        Assert.Equal("Café", dbTalhao.Cultura);
    }

    [Fact]
    public async Task GetTalhoes_ReturnsUnauthorized_WhenUserNotAuthenticated()
    {
        // Arrange
        using var context = new PropriedadesDbContext(CreateNewContextOptions());
        var controller = CreateController(context, userId: null);

        // Act
        var result = await controller.GetTalhoes(Guid.NewGuid());

        // Assert
        Assert.IsType<UnauthorizedResult>(result.Result);
    }

    [Fact]
    public async Task CreateTalhao_ReturnsUnauthorized_WhenUserNotAuthenticated()
    {
        // Arrange
        using var context = new PropriedadesDbContext(CreateNewContextOptions());
        var controller = CreateController(context, userId: null);
        var dto = new CreateTalhaoDto("Talhao 1", "Soja", 10.5m);

        // Act
        var result = await controller.CreateTalhao(Guid.NewGuid(), dto);

        // Assert
        Assert.IsType<UnauthorizedResult>(result.Result);
    }
}
