namespace AgroSolutions.Analise.WebApi.Infrastructure.SqlServer;

public class SqlServerOptions
{
    public const string SectionName = "SqlServer";
    public string ConnectionString { get; set; } = string.Empty;
}
