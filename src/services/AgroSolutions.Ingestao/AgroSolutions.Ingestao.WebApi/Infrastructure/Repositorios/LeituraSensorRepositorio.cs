using AgroSolutions.Ingestao.WebApi.Domain;
using AgroSolutions.Ingestao.WebApi.Infrastructure.SqlServer;
using Dapper;

namespace AgroSolutions.Ingestao.WebApi.Infrastructure.Repositorios;

public sealed class LeituraSensorRepositorio : ILeituraSensorRepositorio
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public LeituraSensorRepositorio(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<long> InserirAsync(LeituraSensor leitura, CancellationToken ct)
    {
        const string sql = @"
INSERT INTO dbo.SensorLeitura
(
    IdPropriedade,
    IdTalhao,
    Origem,
    DataHoraCapturaUtc,
    UmidadeSoloPercentual,
    TemperaturaCelsius,
    PrecipitacaoMilimetros,
    IdDispositivo,
    CorrelationId
)
VALUES
(
    @IdPropriedade,
    @IdTalhao,
    @Origem,
    @DataHoraCapturaUtc,
    @UmidadeSoloPercentual,
    @TemperaturaCelsius,
    @PrecipitacaoMilimetros,
    @IdDispositivo,
    @CorrelationId
);

SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";

        await using var conn = _connectionFactory.Create();
        var id = await conn.ExecuteScalarAsync<long>(new CommandDefinition(
            sql, leitura, cancellationToken: ct));

        return id;
    }

    public async Task<IReadOnlyList<LeituraSensor>> ConsultarAsync(
        Guid idTalhao,
        DateTime deUtc,
        DateTime ateUtc,
        int? agruparMinutos,
        CancellationToken ct)
    {
        // Observação: para o Grafana, geralmente é melhor retornar série agregada para reduzir pontos.
        if (agruparMinutos is null || agruparMinutos <= 0)
        {
            const string sqlRaw = @"
SELECT
    Id,
    IdPropriedade,
    IdTalhao,
    Origem,
    DataHoraCapturaUtc,
    UmidadeSoloPercentual,
    TemperaturaCelsius,
    PrecipitacaoMilimetros,
    IdDispositivo,
    CorrelationId
FROM dbo.SensorLeitura
WHERE IdTalhao = @IdTalhao
  AND DataHoraCapturaUtc >= @DeUtc
  AND DataHoraCapturaUtc <  @AteUtc
ORDER BY DataHoraCapturaUtc ASC;";

            await using var conn = _connectionFactory.Create();
            var rows = await conn.QueryAsync<LeituraSensor>(new CommandDefinition(
                sqlRaw, new { IdTalhao = idTalhao, DeUtc = deUtc, AteUtc = ateUtc }, cancellationToken: ct));

            return rows.ToList();
        }

        // Bucket por N minutos
        // Nota: dateadd/minute/diff com 0 (1900-01-01) é padrão em SQL Server.
        var bucket = agruparMinutos.Value;

        var sqlAgg = $@"
SELECT
    0 AS Id,
    MIN(IdPropriedade) AS IdPropriedade,
    IdTalhao,
    'agregado' AS Origem,
    DATEADD(minute, DATEDIFF(minute, 0, DataHoraCapturaUtc) / {bucket} * {bucket}, 0) AS DataHoraCapturaUtc,
    AVG(CAST(UmidadeSoloPercentual AS float)) AS UmidadeSoloPercentual,
    AVG(CAST(TemperaturaCelsius AS float)) AS TemperaturaCelsius,
    SUM(CAST(PrecipitacaoMilimetros AS float)) AS PrecipitacaoMilimetros,
    NULL AS IdDispositivo,
    NULL AS CorrelationId
FROM dbo.SensorLeitura
WHERE IdTalhao = @IdTalhao
  AND DataHoraCapturaUtc >= @DeUtc
  AND DataHoraCapturaUtc <  @AteUtc
GROUP BY
    IdTalhao,
    DATEADD(minute, DATEDIFF(minute, 0, DataHoraCapturaUtc) / {bucket} * {bucket}, 0)
ORDER BY DataHoraCapturaUtc ASC;";

        await using var conn2 = _connectionFactory.Create();
        var rowsAgg = await conn2.QueryAsync<LeituraSensor>(new CommandDefinition(
            sqlAgg, new { IdTalhao = idTalhao, DeUtc = deUtc, AteUtc = ateUtc }, cancellationToken: ct));

        return rowsAgg.ToList();
    }
}
