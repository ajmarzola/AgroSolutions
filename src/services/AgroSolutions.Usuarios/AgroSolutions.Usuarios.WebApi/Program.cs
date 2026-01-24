using OpenTelemetry.Metrics;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHealthChecks();

// OpenTelemetry Metrics + Prometheus exporter
builder.Services.AddOpenTelemetry().WithMetrics(metrics =>
{
    metrics
        // Métricas HTTP do ASP.NET Core (latência, contagem, status code, etc.)
        .AddAspNetCoreInstrumentation()
        // Métricas de HttpClient (se a API chama outras APIs)
        .AddHttpClientInstrumentation()
        // Métricas do runtime .NET (GC, threads, etc.)
        .AddRuntimeInstrumentation()
        // Exporter Prometheus
        .AddPrometheusExporter();
});

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