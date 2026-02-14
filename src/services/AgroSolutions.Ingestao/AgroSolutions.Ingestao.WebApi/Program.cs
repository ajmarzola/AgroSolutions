using AgroSolutions.Ingestao.WebApi.Infrastructure.Mensageria;
using AgroSolutions.Ingestao.WebApi.Infrastructure.Repositorios;
using AgroSolutions.Ingestao.WebApi.Infrastructure.Observability;
using AgroSolutions.Ingestao.WebApi.Infrastructure.SqlServer;
using OpenTelemetry.Metrics;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Formatting.Compact;

using AgroSolutions.Ingestao.WebApi.Infrastructure.Database; // Migrations extension

var builder = WebApplication.CreateBuilder(args);

// Serilog Configuration
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithSpan()
    .WriteTo.Console(new RenderedCompactJsonFormatter())
    .CreateLogger();

builder.Host.UseSerilog();


// Observability
builder.Services.AddSingleton<IngestaoMetrics>();

// Controllers (mantém padrão mais amigável para o time)
builder.Services.AddControllers();
builder.Services.AddProblemDetails();

// HTTP Clients
builder.Services.AddHttpClient<AgroSolutions.Ingestao.WebApi.Infrastructure.Services.IPropriedadesService, AgroSolutions.Ingestao.WebApi.Infrastructure.Services.PropriedadesService>(client =>
{
    var url = builder.Configuration["Services:PropriedadesUrl"] ?? "http://propriedades.api"; // Default K8s service name guess
    client.BaseAddress = new Uri(url);
});

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "AgroSolutions.Ingestao", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Informe: Bearer {seu_token}"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Authentication
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrEmpty(jwtKey))
{
    throw new Exception("Jwt:Key is missing in configuration");
}

var key = Encoding.ASCII.GetBytes(jwtKey);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true
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
            .AddSqlClientInstrumentation()
            .AddOtlpExporter(opt =>
            {
                opt.Endpoint = new Uri(builder.Configuration["OpenTelemetry:Endpoint"] ?? "http://localhost:4317");
            });
    });
}

var app = builder.Build();

// Executa Migrations (DbUp)
app.MigrateDatabase();

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

Log.Information("{@StartupInfo}", new { Message = "Startup Completed", Service = "AgroSolutions.Ingestao.WebApi", OpenTelemetry = true, Serilog = true });

await app.RunAsync();
