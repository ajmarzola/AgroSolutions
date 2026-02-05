using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AgroSolutions.Ingestao.Simulador;

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
