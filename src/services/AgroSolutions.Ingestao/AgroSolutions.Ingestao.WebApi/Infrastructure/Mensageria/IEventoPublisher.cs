using AgroSolutions.Ingestao.WebApi.Domain;

namespace AgroSolutions.Ingestao.WebApi.Infrastructure.Mensageria;

public interface IEventoPublisher
{
    Task PublicarLeituraRecebidaAsync(LeituraSensor leitura, CancellationToken ct);
}
