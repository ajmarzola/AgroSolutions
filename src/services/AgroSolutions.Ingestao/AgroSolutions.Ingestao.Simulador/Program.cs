using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AgroSolutions.Ingestao.Simulador;
using OpenTelemetry;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;

// Configuração OpenTelemetry (Tracing)
var otelEnabled = Environment.GetEnvironmentVariable("OpenTelemetry_Enabled") == "true" || true; // Default true or check config
var otelEndpoint = Environment.GetEnvironmentVariable("OpenTelemetry_Endpoint") ?? "http://localhost:4317";

using var tracerProvider = otelEnabled ? Sdk.CreateTracerProviderBuilder()
    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("AgroSolutions.Ingestao.Simulador"))
    .AddHttpClientInstrumentation()
    .AddOtlpExporter(opt => opt.Endpoint = new Uri(otelEndpoint))
    .Build() : null;

var options = SimuladorOptions.FromArgs(args);


var jsonOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
};

using var http = new HttpClient
{
    BaseAddress = new UriBuilder(options.BaseUrl).Uri
};

if (!string.IsNullOrWhiteSpace(options.BearerToken))
{
    http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.BearerToken);
}

Console.WriteLine("AgroSolutions.Ingestao.Simulador");
Console.WriteLine($"BaseUrl: {http.BaseAddress}");
Console.WriteLine($"Propriedade: {options.IdPropriedade}");
Console.WriteLine($"Talhoes: {string.Join(", ", options.Talhoes)}");
Console.WriteLine($"Intervalo: {options.IntervaloSeconds}s | TotalPorTalhao: {options.TotalPorTalhao}");
Console.WriteLine($"Fonte: {options.Fonte} | Dispositivo: {options.IdDispositivo}");
Console.WriteLine();

var rnd = new Random(options.Seed);

for (var i = 0; i < options.TotalPorTalhao; i++)
{
    foreach (var idTalhao in options.Talhoes)
    {
        // Simula multi-propriedade para variar métricas de negócio
        var currentOptions = options;
        // Se a variável de ambiente MULTI_PROPRIEDADE=true, varia IDs
        if (Environment.GetEnvironmentVariable("MULTI_PROPRIEDADE") == "true")
        {
             // 50% de chance de mudar a propriedade para gerar volume distribuído
             if (rnd.NextDouble() > 0.5) 
             {
                 var suffix = rnd.Next(2, 6); // ex: ...0002 a ...0005
                 var randomProp = Guid.Parse($"00000000-0000-0000-0000-00000000000{suffix}");
                 currentOptions = options with { IdPropriedade = randomProp };
             }
        }

        var leitura = LeituraSensorDto.CriarAleatoria(idTalhao, currentOptions, rnd);

        var content = new StringContent(JsonSerializer.Serialize(leitura, jsonOptions), Encoding.UTF8, "application/json");

        try
        {
            var resp = await http.PostAsync("api/v1/leituras-sensores", content);
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                Console.WriteLine($"[ERRO] Talhao={idTalhao} Status={(int)resp.StatusCode} {resp.ReasonPhrase} Body={body}");
            }
            else
            {
                Console.WriteLine($"[OK] Talhao={idTalhao} CapturaUtc={leitura.DataHoraCapturaUtc:o} Umidade={leitura.Metricas.UmidadeSoloPercentual}% Temp={leitura.Metricas.TemperaturaCelsius}C Chuva={leitura.Metricas.PrecipitacaoMilimetros}mm");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EXCECAO] Talhao={idTalhao} {ex.GetType().Name}: {ex.Message}");
        }
    }

    if (i < options.TotalPorTalhao - 1)
        await Task.Delay(TimeSpan.FromSeconds(options.IntervaloSeconds));
}

Console.WriteLine();
Console.WriteLine("Finalizado.");
