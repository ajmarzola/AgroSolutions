namespace AgroSolutions.Propriedades.WebApi.Entities;

public class Talhao
{
    public Guid Id { get; set; }
    public Guid PropriedadeId { get; set; }
    public Propriedade? Propriedade { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Cultura { get; set; } = string.Empty;
    public decimal Area { get; set; }
}
