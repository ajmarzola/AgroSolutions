namespace AgroSolutions.Ingestao.WebApi.Infrastructure.SqlServer;

public sealed class SqlServerOptions
{
    public const string SectionName = "SqlServer";

    public string ConnectionString { get; init; } = default!;
}
