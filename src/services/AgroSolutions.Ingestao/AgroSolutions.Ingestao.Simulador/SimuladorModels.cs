using System.Text.Json.Serialization;

namespace AgroSolutions.Ingestao.Simulador;

internal sealed record SimuladorOptions(
    string BaseUrl,
    Guid IdPropriedade,
    IReadOnlyList<Guid> Talhoes,
    int IntervaloSeconds,
    int TotalPorTalhao,
    string Fonte,
    string IdDispositivo,
    string? BearerToken,
    string? UserEmail,
    string? UserPassword,
    string AuthBaseUrl,
    int Seed,
    decimal UmidadeMin,
    decimal UmidadeMax,
    decimal TemperaturaMin,
    decimal TemperaturaMax,
    decimal PrecipitacaoMin,
    decimal PrecipitacaoMax,
    string? SimulationConfigPath,
    string PropriedadesBaseUrl,
    string? PropriedadesConnectionString,
    string? UsuariosConnectionString
)
{
    public static SimuladorOptions FromArgs(string[] args)
    {
        // Defaults (podem ser sobrescritos por env vars e/ou args)
        var baseUrl = Environment.GetEnvironmentVariable("INGESTAO_BASE_URL") ?? BuildDefaultBaseUrl();
        var propriedadesBaseUrl = Environment.GetEnvironmentVariable("PROPRIEDADES_BASE_URL") ?? "http://localhost:30002";
        var propriedadesConnString = Environment.GetEnvironmentVariable("PROPRIEDADES_CONNECTION_STRING") ?? "Server=localhost,1433;Database=AgroSolutionsPropriedades;User Id=sa;Password=Fi@p2026;TrustServerCertificate=True;";
        var usuariosConnString = Environment.GetEnvironmentVariable("USUARIOS_CONNECTION_STRING") ?? "Server=localhost,1433;Database=AgroSolutionsUsuarios;User Id=sa;Password=Fi@p2026;TrustServerCertificate=True;";
        var talhoesCsv = Environment.GetEnvironmentVariable("TALHOES") ?? "";
        var idPropriedadeRaw = Environment.GetEnvironmentVariable("ID_PROPRIEDADE") ?? "00000000-0000-0000-0000-000000000001";
        var intervalo = TryInt(Environment.GetEnvironmentVariable("INTERVALO_SECONDS"), 5);
        var total = TryInt(Environment.GetEnvironmentVariable("TOTAL_POR_TALHAO"), 12);
        var fonte = Environment.GetEnvironmentVariable("FONTE") ?? "simulador";
        var idDispositivo = Environment.GetEnvironmentVariable("ID_DISPOSITIVO") ?? "SIM-001";
        var token = Environment.GetEnvironmentVariable("BEARER_TOKEN");
        var userEmail = Environment.GetEnvironmentVariable("SIM_USER_EMAIL");
        var userPassword = Environment.GetEnvironmentVariable("SIM_USER_PASSWORD");
        var authBaseUrl = Environment.GetEnvironmentVariable("AUTH_BASE_URL") ?? "http://localhost:30001";
        var seed = TryInt(Environment.GetEnvironmentVariable("SEED"), Environment.TickCount);
        var simConfigPath = Environment.GetEnvironmentVariable("SIMULATION_CONFIG_PATH");

        var umidadeMin = TryDec(Environment.GetEnvironmentVariable("UMIDADE_MIN"), 25m);
        var umidadeMax = TryDec(Environment.GetEnvironmentVariable("UMIDADE_MAX"), 75m);
        var tempMin = TryDec(Environment.GetEnvironmentVariable("TEMPERATURA_MIN"), 18m);
        var tempMax = TryDec(Environment.GetEnvironmentVariable("TEMPERATURA_MAX"), 34m);
        var precMin = TryDec(Environment.GetEnvironmentVariable("PRECIPITACAO_MIN"), 0m);
        var precMax = TryDec(Environment.GetEnvironmentVariable("PRECIPITACAO_MAX"), 12m);

        // Args (superam env vars). Formato: --key=value
        foreach (var a in args)
        {
            if (!a.StartsWith("--", StringComparison.Ordinal))
            {
                continue;
            }

            var parts = a.Substring(2).Split('=', 2);
            if (parts.Length != 2)
            {
                continue;
            }

            var key = parts[0].Trim().ToLowerInvariant();
            var value = parts[1].Trim();

            switch (key)
            {
                case "baseurl": baseUrl = value; break;
                case "propriedadesurl": propriedadesBaseUrl = value; break;
                case "talhoes": talhoesCsv = value; break;
                case "propriedade": idPropriedadeRaw = value; break;
                case "idpropriedade": idPropriedadeRaw = value; break;
                case "intervalo": intervalo = TryInt(value, intervalo); break;
                case "total": total = TryInt(value, total); break;
                case "email": userEmail = value; break;
                case "password": userPassword = value; break;
                case "authurl": authBaseUrl = value; break;
                case "fonte": fonte = value; break;
                case "dispositivo": idDispositivo = value; break;
                case "token": token = value; break;
                case "seed": seed = TryInt(value, seed); break;
                case "config": simConfigPath = value; break;

                case "umidadeMin": umidadeMin = TryDec(value, umidadeMin); break;
                case "umidadeMax": umidadeMax = TryDec(value, umidadeMax); break;
                case "temperaturaMin": tempMin = TryDec(value, tempMin); break;
                case "temperaturaMax": tempMax = TryDec(value, tempMax); break;
                case "precipitacaoMin": precMin = TryDec(value, precMin); break;
                case "precipitacaoMax": precMax = TryDec(value, precMax); break;
            }
        }

        var idPropriedade = ParseGuidOrDeterministic(idPropriedadeRaw);

        var talhoes = talhoesCsv
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(ParseGuidOrDeterministicOrNull)
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .Distinct()
            .ToList();

        // Defaults to empty instead of hardcoded if we want to fetch dynamically
        // But for backward compat/specific runs maybe we verify count later.

        return new SimuladorOptions(
            BaseUrl: baseUrl,
            IdPropriedade: idPropriedade,
            Talhoes: talhoes,
            IntervaloSeconds: intervalo,
            TotalPorTalhao: total,
            Fonte: fonte,
            IdDispositivo: idDispositivo,
            BearerToken: token,
            UserEmail: userEmail,
            UserPassword: userPassword,
            AuthBaseUrl: authBaseUrl,
            Seed: seed,
            UmidadeMin: umidadeMin,
            UmidadeMax: umidadeMax,
            TemperaturaMin: tempMin,
            TemperaturaMax: tempMax,
            PrecipitacaoMin: precMin,
            PrecipitacaoMax: precMax,
            SimulationConfigPath: simConfigPath,
            PropriedadesBaseUrl: propriedadesBaseUrl,
            PropriedadesConnectionString: propriedadesConnString,
            UsuariosConnectionString: usuariosConnString
        );
    }

    private static string BuildDefaultBaseUrl()
    {
        var uri = new UriBuilder(Uri.UriSchemeHttp, "localhost", 5081).Uri;
        return uri.ToString();
    }

    private static int TryInt(string? value, int fallback) => int.TryParse(value, out var x) ? x : fallback;
    private static decimal TryDec(string? value, decimal fallback) => decimal.TryParse(value, out var x) ? x : fallback;

    private static Guid ParseGuidOrDeterministic(string value)
    {
        if (Guid.TryParse(value, out var guid))
        {
            return guid;
        }

        if (int.TryParse(value, out var num))
        {
            return Guid.Parse($"00000000-0000-0000-0000-{num:D12}");
        }

        return Guid.Parse("00000000-0000-0000-0000-000000000001");
    }

    private static Guid? ParseGuidOrDeterministicOrNull(string value)
    {
        if (Guid.TryParse(value, out var guid))
        {
            return guid;
        }

        if (int.TryParse(value, out var num))
        {
            return Guid.Parse($"00000000-0000-0000-0000-{num:D12}");
        }

        return null;
    }
}

internal sealed record LeituraSensorDto(
    [property: JsonPropertyName("idPropriedade")] Guid IdPropriedade,
    [property: JsonPropertyName("idTalhao")] Guid IdTalhao,
    [property: JsonPropertyName("origem")] string Origem,
    [property: JsonPropertyName("dataHoraCapturaUtc")] DateTime DataHoraCapturaUtc,
    [property: JsonPropertyName("metricas")] MetricasDto Metricas,
    [property: JsonPropertyName("meta")] MetaDto Meta
)
{
    public static LeituraSensorDto CriarAleatoria(Guid idTalhao, SimuladorOptions options, Random rnd)
    {
        // Pequena variação para ficar "orgânico"
        var now = DateTime.UtcNow;

        // Novos parâmetros conforme solicitação:
        // Umidade do Solo: 15% a 40%
        var umidade = NextRange(rnd, 15m, 40m);

        // Temperatura Ambiente: 18°C a 35°C
        var temp = NextRange(rnd, 18m, 35m);

        // Nível de Nitrogênio (N): 20 a 50 mg/kg
        var nitrogenio = NextRange(rnd, 20m, 50m);

        // Status do Sensor: 95% de chance de "Ativo", 5% de "Falha de Leitura"
        var status = rnd.NextDouble() < 0.95 ? "Ativo" : "Falha de Leitura";

        // Chuva tende a ser 0 na maioria das vezes, com "picos" ocasionais
        var chuva = rnd.NextDouble() < 0.70
            ? 0m
            : NextRange(rnd, options.PrecipitacaoMin, options.PrecipitacaoMax);

        // Se falha de leitura, talvez os valores devessem ser nulos? 
        // A solicitação não especificou, mas vamos manter os valores gerados.

        return new LeituraSensorDto(
            options.IdPropriedade,
            idTalhao,
            options.Fonte,
            now,
            new MetricasDto(
                Round2(umidade),
                Round2(temp),
                Round2(chuva),
                Round2(nitrogenio),
                status),
            new MetaDto(
                options.IdDispositivo,
                Guid.NewGuid().ToString("N"))
        );
    }

    private static decimal NextRange(Random rnd, decimal min, decimal max)
    {
        if (max <= min)
        {
            return min;
        }

        var r = (decimal)rnd.NextDouble();
        return min + (r * (max - min));
    }

    private static decimal Round2(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);
}

internal sealed record MetricasDto(
    [property: JsonPropertyName("umidadeSoloPercentual")] decimal? UmidadeSoloPercentual,
    [property: JsonPropertyName("temperaturaCelsius")] decimal? TemperaturaCelsius,
    [property: JsonPropertyName("precipitacaoMilimetros")] decimal? PrecipitacaoMilimetros,
    [property: JsonPropertyName("nivelNitrogenio")] decimal? NivelNitrogenio,
    [property: JsonPropertyName("statusSensor")] string? StatusSensor
);

internal sealed record MetaDto(
    [property: JsonPropertyName("idDispositivo")] string IdDispositivo,
    [property: JsonPropertyName("correlationId")] string CorrelationId
);

internal sealed record SimulationUserConfig(
    string Email,
    string Password,
    string? Token,
    List<SimulationPropertyConfig> Properties
)
{
    public string? Token { get; set; } = Token;
}


internal sealed record SimulationPropertyConfig(
    Guid Id,
    string Nome,
    List<Guid> Talhoes
);

