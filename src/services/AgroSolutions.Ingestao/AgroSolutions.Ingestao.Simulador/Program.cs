using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

var options = SimuladorOptions.FromArgs(args);

var jsonOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
};

using var http = new HttpClient
{
    BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/")
};

if (!string.IsNullOrWhiteSpace(options.BearerToken))
{
    http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.BearerToken);
}

Console.WriteLine("AgroSolutions.Ingestao.Simulador");
Console.WriteLine($"BaseUrl: {http.BaseAddress}");
Console.WriteLine($"Talhoes: {string.Join(", ", options.Talhoes)}");
Console.WriteLine($"Intervalo: {options.IntervaloSeconds}s | TotalPorTalhao: {options.TotalPorTalhao}");
Console.WriteLine($"Fonte: {options.Fonte} | Dispositivo: {options.IdDispositivo}");
Console.WriteLine();

var rnd = new Random(options.Seed);

for (var i = 0; i < options.TotalPorTalhao; i++)
{
    foreach (var idTalhao in options.Talhoes)
    {
        var leitura = LeituraSensorDto.CriarAleatoria(idTalhao, options, rnd);

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
                Console.WriteLine($"[OK] Talhao={idTalhao} CapturaUtc={leitura.DataHoraCapturaUtc:o} Umidade={leitura.UmidadeSolo}% Temp={leitura.TemperaturaC}C Chuva={leitura.PrecipitacaoMm}mm");
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

internal sealed record SimuladorOptions(
    string BaseUrl,
    IReadOnlyList<int> Talhoes,
    int IntervaloSeconds,
    int TotalPorTalhao,
    string Fonte,
    string IdDispositivo,
    string? BearerToken,
    int Seed,
    decimal UmidadeMin,
    decimal UmidadeMax,
    decimal TemperaturaMin,
    decimal TemperaturaMax,
    decimal PrecipitacaoMin,
    decimal PrecipitacaoMax
)
{
    public static SimuladorOptions FromArgs(string[] args)
    {
        // Defaults (podem ser sobrescritos por env vars e/ou args)
        var baseUrl = Environment.GetEnvironmentVariable("INGESTAO_BASE_URL") ?? "http://localhost:8080";
        var talhoesCsv = Environment.GetEnvironmentVariable("TALHOES") ?? "1";
        var intervalo = TryInt(Environment.GetEnvironmentVariable("INTERVALO_SECONDS"), 5);
        var total = TryInt(Environment.GetEnvironmentVariable("TOTAL_POR_TALHAO"), 12);
        var fonte = Environment.GetEnvironmentVariable("FONTE") ?? "simulador";
        var idDispositivo = Environment.GetEnvironmentVariable("ID_DISPOSITIVO") ?? "SIM-001";
        var token = Environment.GetEnvironmentVariable("BEARER_TOKEN");
        var seed = TryInt(Environment.GetEnvironmentVariable("SEED"), Environment.TickCount);

        var umidadeMin = TryDec(Environment.GetEnvironmentVariable("UMIDADE_MIN"), 25m);
        var umidadeMax = TryDec(Environment.GetEnvironmentVariable("UMIDADE_MAX"), 75m);
        var tempMin = TryDec(Environment.GetEnvironmentVariable("TEMPERATURA_MIN"), 18m);
        var tempMax = TryDec(Environment.GetEnvironmentVariable("TEMPERATURA_MAX"), 34m);
        var precMin = TryDec(Environment.GetEnvironmentVariable("PRECIPITACAO_MIN"), 0m);
        var precMax = TryDec(Environment.GetEnvironmentVariable("PRECIPITACAO_MAX"), 12m);

        // Args (superam env vars). Formato: --key=value
        foreach (var a in args)
        {
            if (!a.StartsWith("--", StringComparison.Ordinal)) continue;
            var parts = a.Substring(2).Split('=', 2);
            if (parts.Length != 2) continue;

            var key = parts[0].Trim().ToLowerInvariant();
            var value = parts[1].Trim();

            switch (key)
            {
                case "baseurl": baseUrl = value; break;
                case "talhoes": talhoesCsv = value; break;
                case "intervalo": intervalo = TryInt(value, intervalo); break;
                case "total": total = TryInt(value, total); break;
                case "fonte": fonte = value; break;
                case "dispositivo": idDispositivo = value; break;
                case "token": token = value; break;
                case "seed": seed = TryInt(value, seed); break;

                case "umidadeMin": umidadeMin = TryDec(value, umidadeMin); break;
                case "umidadeMax": umidadeMax = TryDec(value, umidadeMax); break;
                case "temperaturaMin": tempMin = TryDec(value, tempMin); break;
                case "temperaturaMax": tempMax = TryDec(value, tempMax); break;
                case "precipitacaoMin": precMin = TryDec(value, precMin); break;
                case "precipitacaoMax": precMax = TryDec(value, precMax); break;
            }
        }

        var talhoes = talhoesCsv
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(v => int.TryParse(v, out var x) ? x : (int?)null)
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .Distinct()
            .ToList();

        if (talhoes.Count == 0) talhoes.Add(1);

        return new SimuladorOptions(
            baseUrl,
            talhoes,
            intervalo,
            total,
            fonte,
            idDispositivo,
            token,
            seed,
            umidadeMin, umidadeMax,
            tempMin, tempMax,
            precMin, precMax
        );
    }

    private static int TryInt(string? value, int fallback) => int.TryParse(value, out var x) ? x : fallback;
    private static decimal TryDec(string? value, decimal fallback) => decimal.TryParse(value, out var x) ? x : fallback;
}

internal sealed record LeituraSensorDto(
    [property: JsonPropertyName("idTalhao")] int IdTalhao,
    [property: JsonPropertyName("dataHoraCapturaUtc")] DateTime DataHoraCapturaUtc,
    [property: JsonPropertyName("umidadeSolo")] decimal? UmidadeSolo,
    [property: JsonPropertyName("temperaturaC")] decimal? TemperaturaC,
    [property: JsonPropertyName("precipitacaoMm")] decimal? PrecipitacaoMm,
    [property: JsonPropertyName("fonte")] string Fonte,
    [property: JsonPropertyName("idDispositivo")] string IdDispositivo
)
{
    public static LeituraSensorDto CriarAleatoria(int idTalhao, SimuladorOptions options, Random rnd)
    {
        // Pequena variação para ficar "orgânico"
        var now = DateTime.UtcNow;

        var umidade = NextRange(rnd, options.UmidadeMin, options.UmidadeMax);
        var temp = NextRange(rnd, options.TemperaturaMin, options.TemperaturaMax);

        // Chuva tende a ser 0 na maioria das vezes, com "picos" ocasionais
        var chuva = rnd.NextDouble() < 0.70
            ? 0m
            : NextRange(rnd, options.PrecipitacaoMin, options.PrecipitacaoMax);

        return new LeituraSensorDto(
            idTalhao,
            now,
            Round2(umidade),
            Round2(temp),
            Round2(chuva),
            options.Fonte,
            options.IdDispositivo
        );
    }

    private static decimal NextRange(Random rnd, decimal min, decimal max)
    {
        if (max <= min) return min;
        var r = (decimal)rnd.NextDouble();
        return min + (r * (max - min));
    }

    private static decimal Round2(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);
}
