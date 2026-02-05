using System.Collections.Concurrent;
using AgroSolutions.Ingestao.WebApi.Domain;

namespace AgroSolutions.Ingestao.WebApi.Infrastructure.Repositorios;

public sealed class InMemoryLeituraSensorRepositorio : ILeituraSensorRepositorio
{
    private static long _id;
    private static readonly ConcurrentDictionary<long, LeituraSensor> Store = new();

    public Task<long> InserirAsync(LeituraSensor leitura, CancellationToken ct)
    {
        var id = Interlocked.Increment(ref _id);

        var copia = new LeituraSensor
        {
            Id = id,
            IdPropriedade = leitura.IdPropriedade,
            IdTalhao = leitura.IdTalhao,
            Origem = leitura.Origem,
            DataHoraCapturaUtc = leitura.DataHoraCapturaUtc,
            UmidadeSoloPercentual = leitura.UmidadeSoloPercentual,
            TemperaturaCelsius = leitura.TemperaturaCelsius,
            PrecipitacaoMilimetros = leitura.PrecipitacaoMilimetros,
            IdDispositivo = leitura.IdDispositivo,
            CorrelationId = leitura.CorrelationId
        };

        Store[id] = copia;
        return Task.FromResult(id);
    }

    public Task<IReadOnlyList<LeituraSensor>> ConsultarAsync(
        Guid idTalhao,
        DateTime deUtc,
        DateTime ateUtc,
        int? agruparMinutos,
        CancellationToken ct)
    {
        var rows = Store.Values
            .Where(x => x.IdTalhao == idTalhao
                        && x.DataHoraCapturaUtc >= deUtc
                        && x.DataHoraCapturaUtc < ateUtc)
            .OrderBy(x => x.DataHoraCapturaUtc)
            .ToList();

        if (agruparMinutos is null || agruparMinutos <= 0)
            return Task.FromResult<IReadOnlyList<LeituraSensor>>(rows);

        var bucket = TimeSpan.FromMinutes(agruparMinutos.Value);

        var aggregated = rows
            .GroupBy(x => TruncarPorIntervalo(x.DataHoraCapturaUtc, bucket))
            .Select(g => new LeituraSensor
            {
                Id = 0,
                IdPropriedade = g.First().IdPropriedade,
                IdTalhao = idTalhao,
                Origem = "agregado",
                DataHoraCapturaUtc = g.Key,
                UmidadeSoloPercentual = Media(g.Select(x => x.UmidadeSoloPercentual)),
                TemperaturaCelsius = Media(g.Select(x => x.TemperaturaCelsius)),
                PrecipitacaoMilimetros = Soma(g.Select(x => x.PrecipitacaoMilimetros)),
                IdDispositivo = null,
                CorrelationId = null
            })
            .OrderBy(x => x.DataHoraCapturaUtc)
            .ToList();

        return Task.FromResult<IReadOnlyList<LeituraSensor>>(aggregated);
    }

    private static DateTime TruncarPorIntervalo(DateTime valueUtc, TimeSpan interval)
    {
        var ticks = valueUtc.Ticks - (valueUtc.Ticks % interval.Ticks);
        return new DateTime(ticks, DateTimeKind.Utc);
    }

    private static decimal? Media(IEnumerable<decimal?> valores)
    {
        var list = valores.Where(v => v.HasValue).Select(v => v!.Value).ToList();
        return list.Count == 0 ? null : list.Average();
    }

    private static decimal? Soma(IEnumerable<decimal?> valores)
    {
        var list = valores.Where(v => v.HasValue).Select(v => v!.Value).ToList();
        return list.Count == 0 ? null : list.Sum();
    }
}
