using DbUp;

namespace AgroSolutions.Analise.WebApi.Infrastructure.Database;

public static class MigrationExtensions
{
    public static void MigrateDatabase(this WebApplication app)
    {
        var connectionString = app.Configuration.GetConnectionString("DefaultConnection");

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
            Console.WriteLine($"Erro ao garantir base de dados: {ex.Message}");
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
