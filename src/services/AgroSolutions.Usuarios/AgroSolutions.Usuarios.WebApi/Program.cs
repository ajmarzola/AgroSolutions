using AgroSolutions.Usuarios.WebApi.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace; // Might be needed for WithTracing
using System.Text;
using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);

// Serilog Configuration
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithSpan()
    .WriteTo.Console(new RenderedCompactJsonFormatter())
    .CreateLogger();

builder.Host.UseSerilog();

// 1. Banco de Dados com Resiliência
var conn = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrEmpty(conn) || builder.Configuration.GetValue<bool>("SqlServer:UseInMemory"))
{
    builder.Services.AddDbContext<AgroDbContext>(options =>
        options.UseInMemoryDatabase("AgroSolutionsUsuariosDb"));
}
else
{
    builder.Services.AddDbContext<AgroDbContext>(options =>
        options.UseSqlServer(conn, sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(30), null);
        }));
}

// 2. Autenticação JWT - Validação da Chave
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrEmpty(jwtKey))
{
    throw new InvalidOperationException("Jwt:Key não encontrada na configuração.");
}
var key = Encoding.ASCII.GetBytes(jwtKey);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

// 3. Health Checks
builder.Services.AddHealthChecks()
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), tags: new[] { "liveness" })
    .AddDbContextCheck<AgroDbContext>("DatabaseConnection", Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy, tags: new[] { "readiness" });


// 4. OpenTelemetry
var otel = builder.Services.AddOpenTelemetry().WithMetrics(metrics =>
{
    metrics.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation()
           .AddRuntimeInstrumentation().AddPrometheusExporter();
});

if (builder.Configuration.GetValue("OpenTelemetry:Enabled", false))
{
    otel.WithTracing(tracing =>
    {
        tracing
            .SetResourceBuilder(OpenTelemetry.Resources.ResourceBuilder.CreateDefault()
                .AddService("AgroSolutions.Usuarios.WebApi"))
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddSqlClientInstrumentation(options => { /* options.SetDbStatementForText = true; */ })
            .AddOtlpExporter(opt =>
            {
                opt.Endpoint = new Uri(builder.Configuration["OpenTelemetry:Endpoint"] ?? "http://localhost:4317");
            });
    });
}

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// 5. Swagger com suporte a JWT (Botão Authorize)
builder.Services.AddSwaggerGen(c => {
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "AgroSolutions API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Insira o token JWT desta maneira: Bearer {seu_token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement {
        {
            new OpenApiSecurityScheme {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            new string[] {}
        }
    });
});

builder.Services.AddAuthorization();

var app = builder.Build();

// Executa Migrations (EF Core)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AgroDbContext>();
    if (db.Database.IsSqlServer())
    {
        db.Database.Migrate();

        if (!db.TiposUsuarios.Any())
        {
            db.TiposUsuarios.AddRange(
                new AgroSolutions.Usuarios.WebApi.Entity.TipoUsuario { Descricao = "Produtor" },
                new AgroSolutions.Usuarios.WebApi.Entity.TipoUsuario { Descricao = "Administrador" }
            );
            db.SaveChanges();
            Log.Information("Seeding Data Completed");
        }
    }
}

app.UseSwagger();
app.UseSwaggerUI(c => {
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "AgroSolutions API v1");
});

app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("liveness")
});
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("readiness")
});
app.MapHealthChecks("/health");

app.UseOpenTelemetryPrometheusScrapingEndpoint();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

Log.Information("{@StartupInfo}", new { Message = "Startup Completed", Service = "AgroSolutions.Usuarios.WebApi", OpenTelemetry = true, Serilog = true });

app.Run();