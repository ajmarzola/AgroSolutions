using AgroSolutions.Analise.WebApi.Domain;

namespace AgroSolutions.Analise.WebApi.Infrastructure.Repositorios;

public interface IAnaliseRepositorio
{
    Task SalvarLeituraAsync(Leitura leitura);
    Task SalvarAlertaAsync(Alerta alerta);
    Task<IEnumerable<Leitura>> GetLeiturasUltimas24HorasAsync(Guid talhaoId);
    Task<IEnumerable<Leitura>> ListarLeiturasAsync(Guid? idTalhao, int top = 100);
    Task<IEnumerable<Alerta>> ListarAlertasAsync(Guid? idTalhao, int top = 100);
}
