using AgroSolutions.Ingestao.WebApi.Domain;

namespace AgroSolutions.Ingestao.WebApi.Infrastructure.Repositorios;

public interface ILeituraSensorRepositorio
{
    Task<long> InserirAsync(LeituraSensor leitura, CancellationToken ct);

    Task<IReadOnlyList<LeituraSensor>> ConsultarAsync(
        Guid idTalhao,
        DateTime deUtc,
        DateTime ateUtc,
        int? agruparMinutos,
        CancellationToken ct);
}
