using DbUp;

namespace AgroSolutions.Ingestao.WebApi.Infrastructure.Database;

public static class MigrationExtensions
{
    public static void MigrateDatabase(this WebApplication app)
    {
        var connectionString = app.Configuration.GetConnectionString("DefaultConnection");

        // Se a connection string estiver vazia ou se usar InMemory (caso haja flag), ignora
        // No caso de Dapper, InMemory é menos comum do que EF Core, mas mantemos o padrão de verificação
        if (string.IsNullOrEmpty(connectionString))
        {
            Console.WriteLine("String de conexão 'DefaultConnection' não encontrada. Pulando migrações.");
            return;
        }

        try 
        {
            EnsureDatabase.For.SqlDatabase(connectionString);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao garantir base de dados (pode ser problema de permissão no master): {ex.Message}");
            // Prossegue pois o banco pode já existir
        }

        var upgrader = DeployChanges.To
            .SqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(typeof(MigrationExtensions).Assembly)
            .LogToConsole()
            .Build();

        var result = upgrader.PerformUpgrade();

        if (!result.Successful)
        {
            throw new Exception("Falha ao executar migrações do DbUp", result.Error);
        }
    }
}
