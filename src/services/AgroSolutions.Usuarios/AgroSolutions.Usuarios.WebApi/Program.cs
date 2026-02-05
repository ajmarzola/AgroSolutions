using AgroSolutions.Usuarios.WebApi.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Metrics;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. Banco de Dados Azure SQL
var conn = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AgroDbContext>(options => options.UseSqlServer(conn));

// 2. Autenticação JWT
var jwtKey = builder.Configuration["Jwt:Key"] ?? "Chave_Mestra_AgroSolutions_2026_Seguranca_Total";
var key = Encoding.ASCII.GetBytes(jwtKey);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero
        };
    });

// 3. Observabilidade (Health Checks)
builder.Services.AddHealthChecks()
    .AddCheck("DatabaseConnection", () => {
        try
        {
            using (var client = new System.Net.Sockets.TcpClient("agrosolutions.database.windows.net", 1433))
                return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy();
        }
        catch
        {
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy();
        }
    });

// 4. Observabilidade (OpenTelemetry + Prometheus)
builder.Services.AddOpenTelemetry().WithMetrics(metrics =>
{
    metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddPrometheusExporter();
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configuração do Swagger para .NET 9
builder.Services.AddSwaggerGen(c => {
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "AgroSolutions API",
        Version = "v1",
        Description = "Microsserviço de Usuários"
    });
});

builder.Services.AddAuthorization();

var app = builder.Build();

// Pipeline do Swagger
app.UseSwagger();
app.UseSwaggerUI(c => {
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "AgroSolutions API v1");
});

// Monitoramento
app.MapHealthChecks("/health");
app.UseOpenTelemetryPrometheusScrapingEndpoint();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();