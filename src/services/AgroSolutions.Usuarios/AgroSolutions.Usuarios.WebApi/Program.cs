var builder = WebApplication.CreateBuilder(args);

var conn = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AgroDbContext>(options => options.UseSqlServer(conn));

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
           // ClockSkew = TimeSpan.Zero // Validação imediata da expiração
        };
    });

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

builder.Services.AddControllers();
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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();