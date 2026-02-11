using AgroSolutions.Propriedades.WebApi.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<PropriedadesDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// JWT Configuration
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "SegredoPadraoParaDesenvolvimentoNaoUseEmProducao123"))
        };
    });

builder.Services.AddHealthChecks();

// OpenTelemetry Metrics + Prometheus exporter
var otel = builder.Services.AddOpenTelemetry().WithMetrics(metrics =>
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

if (builder.Configuration.GetValue("OpenTelemetry:Enabled", false))
{
    otel.WithTracing(tracing =>
    {
        tracing
            .SetResourceBuilder(ResourceBuilder.CreateDefault()
                .AddService("AgroSolutions.Propriedades.WebApi"))
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddSqlClientInstrumentation(options =>
            {
                options.RecordException = true;
            })
            .AddOtlpExporter(opt =>
            {
                opt.Endpoint = new Uri(builder.Configuration["OpenTelemetry:Endpoint"] ?? "http://localhost:4317");
            });
    });
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection(); // Usually disabled behind k8s ingress/gateway unless required

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");
app.MapPrometheusScrapingEndpoint();

app.Run();
