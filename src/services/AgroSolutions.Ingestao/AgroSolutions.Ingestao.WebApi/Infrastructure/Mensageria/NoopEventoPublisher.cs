using AgroSolutions.Ingestao.WebApi.Domain;

namespace AgroSolutions.Ingestao.WebApi.Infrastructure.Mensageria;

public sealed class NoopEventoPublisher : IEventoPublisher
{
    public Task PublicarLeituraRecebidaAsync(LeituraSensor leitura, CancellationToken ct)
    {
        return Task.CompletedTask;
    }
}
