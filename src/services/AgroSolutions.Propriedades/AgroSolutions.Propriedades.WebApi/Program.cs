using AgroSolutions.Propriedades.WebApi.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Text;
using Serilog;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);

// Serilog Configuration
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    //.Enrich.WithSpan()
    .WriteTo.Console(new RenderedCompactJsonFormatter())
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<PropriedadesDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// JWT Configuration
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrEmpty(jwtKey)) throw new Exception("Jwt:Key is missing in configuration");
var key = Encoding.UTF8.GetBytes(jwtKey);

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
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };
    });

builder.Services.AddHealthChecks()
    .AddDbContextCheck<PropriedadesDbContext>();

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


app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = _ => true
});

app.MapPrometheusScrapingEndpoint();

// Auto-Migration (Hackathon Mode)
if (app.Configuration.GetValue<bool>("Db:AutoMigrate"))
{
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<PropriedadesDbContext>();
        context.Database.Migrate();
    }
}


app.Run();
