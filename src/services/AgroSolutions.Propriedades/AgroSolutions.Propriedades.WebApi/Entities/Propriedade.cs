namespace AgroSolutions.Propriedades.WebApi.Entities;

public class Propriedade
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Localizacao { get; set; } = string.Empty;
    public ICollection<Talhao> Talhoes { get; set; } = new List<Talhao>();
}
