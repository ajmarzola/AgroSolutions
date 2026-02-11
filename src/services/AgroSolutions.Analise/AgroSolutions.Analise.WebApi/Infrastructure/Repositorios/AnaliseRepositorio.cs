using AgroSolutions.Analise.WebApi.Domain;
using AgroSolutions.Analise.WebApi.Infrastructure.SqlServer;
using Dapper;

namespace AgroSolutions.Analise.WebApi.Infrastructure.Repositorios;

public sealed class AnaliseRepositorio : IAnaliseRepositorio
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public AnaliseRepositorio(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task SalvarLeituraAsync(Leitura leitura)
    {
        const string sql = @"
            IF OBJECT_ID('dbo.Leitura', 'U') IS NULL
            BEGIN
               CREATE TABLE dbo.Leitura (
                   Id BIGINT IDENTITY(1,1) PRIMARY KEY,
                   IdTalhao UNIQUEIDENTIFIER NOT NULL,
                   DataHoraCapturaUtc DATETIME2 NOT NULL,
                   TemperaturaCelsius DECIMAL(5,2) NULL,
                   UmidadeSoloPercentual DECIMAL(5,2) NULL,
                   PrecipitacaoMilimetros DECIMAL(5,2) NULL
               );
               CREATE INDEX IX_Leitura_IdTalhao ON dbo.Leitura(IdTalhao);
            END

            INSERT INTO dbo.Leitura (IdTalhao, DataHoraCapturaUtc, TemperaturaCelsius, UmidadeSoloPercentual, PrecipitacaoMilimetros)
            VALUES (@IdTalhao, @DataHoraCapturaUtc, @TemperaturaCelsius, @UmidadeSoloPercentual, @PrecipitacaoMilimetros);";

        // NOTA: O script de criação de tabela acima é apenas para facilitar a POC. Em produção, use Migrations.
        // Assumindo que o banco de dados 'Analise' já existe.

        await using var conn = _connectionFactory.Create();
        await conn.ExecuteAsync(sql, leitura);
    }

    public async Task SalvarAlertaAsync(Alerta alerta)
    {
        const string sql = @"
            IF OBJECT_ID('dbo.Alerta', 'U') IS NULL
            BEGIN
               CREATE TABLE dbo.Alerta (
                   Id BIGINT IDENTITY(1,1) PRIMARY KEY,
                   IdTalhao UNIQUEIDENTIFIER NOT NULL,
                   Mensagem NVARCHAR(500) NOT NULL,
                   Nivel VARCHAR(20) NOT NULL,
                   DataHoraGeracaoUtc DATETIME2 NOT NULL,
                   LeituraId BIGINT NULL
               );
               CREATE INDEX IX_Alerta_IdTalhao ON dbo.Alerta(IdTalhao);
            END

            INSERT INTO dbo.Alerta (IdTalhao, Mensagem, Nivel, DataHoraGeracaoUtc, LeituraId)
            VALUES (@IdTalhao, @Mensagem, @Nivel, @DataHoraGeracaoUtc, @LeituraId);";

        await using var conn = _connectionFactory.Create();
        await conn.ExecuteAsync(sql, alerta);
    }

    public async Task<IEnumerable<Leitura>> GetLeiturasUltimas24HorasAsync(Guid talhaoId)
    {
        const string sql = @"
            SELECT * FROM dbo.Leitura 
            WHERE IdTalhao = @IdTalhao 
            AND DataHoraCapturaUtc >= DATEADD(hour, -24, GETUTCDATE())
        ";
        await using var conn = _connectionFactory.Create();
        return await conn.QueryAsync<Leitura>(sql, new { IdTalhao = talhaoId });
    }

    public async Task<IEnumerable<Leitura>> ListarLeiturasAsync(Guid? idTalhao, int top = 100)
    {
        var sql = "SELECT TOP (@Top) * FROM dbo.Leitura";
        if (idTalhao.HasValue)
        {
            sql += " WHERE IdTalhao = @IdTalhao";
        }
        sql += " ORDER BY DataHoraCapturaUtc DESC";

        await using var conn = _connectionFactory.Create();
        return await conn.QueryAsync<Leitura>(sql, new { IdTalhao = idTalhao, Top = top });
    }

    public async Task<IEnumerable<Alerta>> ListarAlertasAsync(Guid? idTalhao, int top = 100)
    {
        var sql = "SELECT TOP (@Top) * FROM dbo.Alerta";
        if (idTalhao.HasValue)
        {
            sql += " WHERE IdTalhao = @IdTalhao";
        }
        sql += " ORDER BY DataHoraGeracaoUtc DESC";

        await using var conn = _connectionFactory.Create();
        return await conn.QueryAsync<Alerta>(sql, new { IdTalhao = idTalhao, Top = top });
    }

    public async Task<bool> ExisteAlertaRecenteAsync(Guid talhaoId, string trechoMensagem, DateTime dataCorte)
    {
        const string sql = @"
            SELECT TOP 1 1 
            FROM dbo.Alerta 
            WHERE IdTalhao = @IdTalhao 
            AND Mensagem LIKE @MsgPattern
            AND DataHoraGeracaoUtc >= @DataCorte
        ";

        await using var conn = _connectionFactory.Create();
        var result = await conn.ExecuteScalarAsync<int?>(sql, new
        {
            IdTalhao = talhaoId,
            MsgPattern = $"%{trechoMensagem}%",
            DataCorte = dataCorte
        });

        return result.HasValue;
    }
}
