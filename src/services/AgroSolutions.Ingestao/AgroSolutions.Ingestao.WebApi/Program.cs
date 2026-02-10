using AgroSolutions.Ingestao.WebApi.Infrastructure.Mensageria;
using AgroSolutions.Ingestao.WebApi.Infrastructure.Repositorios;
using AgroSolutions.Ingestao.WebApi.Infrastructure.Observability;
using AgroSolutions.Ingestao.WebApi.Infrastructure.SqlServer;
using OpenTelemetry.Metrics;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);


// Observability
builder.Services.AddSingleton<IngestaoMetrics>();

// Controllers (mantém padrão mais amigável para o time)
builder.Services.AddControllers();

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Insira o token JWT: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            new string[] {}
        }
    });
});

// Authentication
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrEmpty(jwtKey)) throw new Exception("Jwt:Key is missing in configuration");
var key = Encoding.ASCII.GetBytes(jwtKey);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = false,
            ValidateAudience = false
        };
    });

// Health checks
builder.Services.AddHealthChecks();

// Options
builder.Services.Configure<SqlServerOptions>(builder.Configuration.GetSection(SqlServerOptions.SectionName));
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.SectionName));

// Infra (SQL Server + RabbitMQ)
builder.Services.AddSingleton(sp => sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<SqlServerOptions>>().Value);

var sqlOptions = builder.Configuration.GetSection(SqlServerOptions.SectionName).Get<SqlServerOptions>() ?? new SqlServerOptions();
var rabbitOptions = builder.Configuration.GetSection(RabbitMqOptions.SectionName).Get<RabbitMqOptions>() ?? new RabbitMqOptions();

if (sqlOptions.UseInMemory)
{
    builder.Services.AddSingleton<ILeituraSensorRepositorio, InMemoryLeituraSensorRepositorio>();
}
else
{
    builder.Services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();
    builder.Services.AddScoped<ILeituraSensorRepositorio, LeituraSensorRepositorio>();
}

if (rabbitOptions.Enabled)
{
    builder.Services.AddSingleton<IEventoPublisher, RabbitMqEventoPublisher>();
}
else
{
    builder.Services.AddSingleton<IEventoPublisher, NoopEventoPublisher>();
}

// OpenTelemetry Metrics + Prometheus exporter
var otel = builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics
            .AddMeter(IngestaoMetrics.MeterName)
            // Métricas HTTP do ASP.NET Core (latência, contagem, status code, etc.)
            .AddAspNetCoreInstrumentation()
            // Métricas de HttpClient (se a API chama outras APIs)
            .AddHttpClientInstrumentation()
            // Métricas do runtime .NET (GC, threads, etc.)
            .AddRuntimeInstrumentation()
            // Exporter Prometheus
            .AddPrometheusExporter();
    });

if (builder.Configuration.GetValue("OpenTelemetry:Enabled", false))
{
    otel.WithTracing(tracing =>
    {
        tracing
            .SetResourceBuilder(OpenTelemetry.Resources.ResourceBuilder.CreateDefault()
                .AddService("AgroSolutions.Ingestao.WebApi"))
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddSqlClientInstrumentation(options => options.SetDbStatementForText = true)
            .AddOtlpExporter(opt =>
            {
                opt.Endpoint = new Uri(builder.Configuration["OpenTelemetry:Endpoint"] ?? "http://localhost:4317");
            });
    });
}

var app = builder.Build();

// HTTP pipeline
app.UseSwagger();
app.UseSwaggerUI();

// Exponha /metrics para Prometheus (endpoint HTTP)
app.MapPrometheusScrapingEndpoint("/metrics");

// Health endpoints
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

await app.RunAsync();
