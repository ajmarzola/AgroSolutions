using Microsoft.Data.SqlClient;

namespace AgroSolutions.Analise.WebApi.Infrastructure.SqlServer;

public interface ISqlConnectionFactory
{
    SqlConnection Create();
}

public sealed class SqlConnectionFactory : ISqlConnectionFactory
{
    private readonly SqlServerOptions _options;

    public SqlConnectionFactory(SqlServerOptions options)
    {
        _options = options;
    }

    public SqlConnection Create()
    {
        if (string.IsNullOrWhiteSpace(_options.ConnectionString))
            throw new InvalidOperationException("SqlServer:ConnectionString não configurada.");

        return new SqlConnection(_options.ConnectionString);
    }
}
