using AgroSolutions.Usuarios.WebApi.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace; // Might be needed for WithTracing
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. Banco de Dados com Resili�ncia
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

// 2. Autentica��o JWT - Valida��o da Chave
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrEmpty(jwtKey)) throw new Exception("Jwt:Key is missing");
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

// 3. Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AgroDbContext>("Database");

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

// 5. Swagger com suporte a JWT (Bot�o Authorize)
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

app.UseSwagger();
app.UseSwaggerUI(c => {
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "AgroSolutions API v1");
});

app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false
});
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => true
});

app.UseOpenTelemetryPrometheusScrapingEndpoint();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();