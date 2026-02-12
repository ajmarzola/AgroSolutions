using AgroSolutions.Analise.WebApi.Infrastructure.Observability;
using AgroSolutions.Analise.WebApi.Infrastructure.Mensageria;
using AgroSolutions.Analise.WebApi.Infrastructure.Repositorios;
using AgroSolutions.Analise.WebApi.Infrastructure.SqlServer;
using AgroSolutions.Analise.WebApi.Infrastructure.HealthChecks;
using AgroSolutions.Analise.WebApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources; 
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);

// Serilog Configuration
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithSpan()
    .WriteTo.Console(new RenderedCompactJsonFormatter())
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddSingleton<AnaliseMetrics>();

// Options
builder.Services.Configure<SqlServerOptions>(builder.Configuration.GetSection(SqlServerOptions.SectionName));
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.SectionName));

// Infra
builder.Services.AddSingleton(sp => sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<SqlServerOptions>>().Value);
builder.Services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();
builder.Services.AddSingleton<IAnaliseRepositorio, AnaliseRepositorio>();

// Services
builder.Services.AddScoped<IMotorDeAlertas, MotorDeAlertas>();

// Consumer (Background Service)
builder.Services.AddHostedService<RabbitMqLeiturasConsumer>();

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
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true
        };
    });

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHealthChecks()
    .AddCheck<SqlServerHealthCheck>("sql");

// OpenTelemetry Metrics + Prometheus exporter
var otel = builder.Services.AddOpenTelemetry().WithMetrics(metrics =>
{
    metrics
        .AddMeter(AnaliseMetrics.MeterName)
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
                .AddService("AgroSolutions.Analise.WebApi"))
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddSqlClientInstrumentation(options => { /* options.SetDbStatementForText = true; */ })
           entication();
app.UseAuth .AddOtlpExporter(opt =>
            {
                opt.Endpoint = new Uri(builder.Configuration["OpenTelemetry:Endpoint"] ?? "http://localhost:4317");
            });
    });
}

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

// Exponha /metrics para Prometheus (endpoint HTTP)
app.MapPrometheusScrapingEndpoint("/metrics");

// (Opcional) Health check básico
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");

app.UseAuthorization();

app.MapControllers();

app.Run();
