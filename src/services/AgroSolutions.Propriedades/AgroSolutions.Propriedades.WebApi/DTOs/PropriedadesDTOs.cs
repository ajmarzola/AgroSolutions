namespace AgroSolutions.Propriedades.WebApi.DTOs;

public record CreatePropriedadeDto(string Nome, string Localizacao);
public record PropriedadeDto(Guid Id, string Nome, string Localizacao);
public record CreateTalhaoDto(string Nome, string Cultura, decimal Area);
public record TalhaoDto(Guid Id, Guid PropriedadeId, string Nome, string Cultura, decimal Area);
