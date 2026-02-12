using System.ComponentModel.DataAnnotations;

namespace AgroSolutions.Propriedades.WebApi.DTOs;

public record CreatePropriedadeDto(
    [Required] string Nome, 
    string Localizacao);

public record PropriedadeDto(Guid Id, string Nome, string Localizacao);

public record CreateTalhaoDto(
    [Required] string Nome, 
    [Required] string Cultura, 
    [Range(0.0001, double.MaxValue)] decimal Area);

public record TalhaoDto(Guid Id, Guid PropriedadeId, string Nome, string Cultura, decimal Area);
