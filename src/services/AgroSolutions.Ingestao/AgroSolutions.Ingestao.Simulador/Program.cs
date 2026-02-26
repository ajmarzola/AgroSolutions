using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using AgroSolutions.Ingestao.Simulador;
using Microsoft.Data.SqlClient;
using OpenTelemetry;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;

// Configuração OpenTelemetry (Tracing)
var otelEnabled = Environment.GetEnvironmentVariable("OpenTelemetry_Enabled") == "true";
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

List<SimulationUserConfig> simulationUsers = new();
if (!string.IsNullOrWhiteSpace(options.SimulationConfigPath) && File.Exists(options.SimulationConfigPath))
{
    try
    {
        var configJson = File.ReadAllText(options.SimulationConfigPath);
        simulationUsers = JsonSerializer.Deserialize<List<SimulationUserConfig>>(configJson, jsonOptions) ?? new();
        Console.WriteLine($"[SETUP] Carregada configuracao de simulacao para {simulationUsers.Count} usuarios do arquivo {options.SimulationConfigPath}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[SETUP-ERRO] Falha ao ler arquivo de configuracao: {ex.Message}");
    }
}

using var http = new HttpClient
{
    BaseAddress = new UriBuilder(options.BaseUrl).Uri
};

// Auth Client
var authClient = new AuthClient(options.AuthBaseUrl);

if (!string.IsNullOrWhiteSpace(options.BearerToken))
{
    http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.BearerToken);
}
else if (!string.IsNullOrWhiteSpace(options.UserEmail) && !string.IsNullOrWhiteSpace(options.UserPassword))
{
    Console.WriteLine($"[AUTH] Tentando autenticar com {options.UserEmail}...");
    var token = await authClient.LoginAsync(options.UserEmail, options.UserPassword);
    if (!string.IsNullOrEmpty(token))
    {
         http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
         Console.WriteLine($"[AUTH] Autenticado com sucesso!");
    }
}

// Propriedades Client
using var propHttp = new HttpClient
{
    BaseAddress = new Uri(options.PropriedadesBaseUrl)
};

Console.WriteLine("AgroSolutions.Ingestao.Simulador");
Console.WriteLine($"BaseUrl: {http.BaseAddress}");
Console.WriteLine($"AuthUrl: {options.AuthBaseUrl}");
Console.WriteLine($"PropriedadesUrl: {options.PropriedadesBaseUrl}");
Console.WriteLine($"UsuariosConn: {(string.IsNullOrEmpty(options.UsuariosConnectionString) ? "N/A" : "Configured")}");
if (simulationUsers.Count == 0)
{
    if (options.Talhoes.Count > 0)
    {
        Console.WriteLine($"Propriedade: {options.IdPropriedade}");
        Console.WriteLine($"Talhoes: {string.Join(", ", options.Talhoes)}");
    }
    else
    {
        Console.WriteLine("Modo Dinâmico: Buscando talhões da API de Propriedades a cada ciclo.");
    }
}
else
{
    Console.WriteLine($"Modo Simulação Massiva: {simulationUsers.Sum(u => u.Properties.Count)} propriedades carregadas.");
}
Console.WriteLine($"Intervalo: {options.IntervaloSeconds}s | TotalPorTalhao: {options.TotalPorTalhao}");
Console.WriteLine($"Fonte: {options.Fonte} | Dispositivo: {options.IdDispositivo}");
Console.WriteLine();

var rnd = new Random(options.Seed);

// Cache de tokens de usuários (email -> token)
var userTokenCache = new Dictionary<string, string>();

async Task<string?> GetTokenForUser(string email, string? fallbackPassword = null)
{
    if (userTokenCache.TryGetValue(email, out var token) && !string.IsNullOrEmpty(token))
    {
        return token;
    }

    var pass = fallbackPassword ?? "Password123!"; // Default fallback
    // Se tivermos a senha real em options.UserPassword e o email bater, usamos ela
    if (email.Equals(options.UserEmail, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(options.UserPassword))
    {
        pass = options.UserPassword;
    }
    
    // Tenta autenticar
    Console.WriteLine($"[AUTH-GET] Tentando obter token para {email}...");
    var newToken = await authClient.LoginAsync(email, pass);
    if (!string.IsNullOrEmpty(newToken))
    {
        userTokenCache[email] = newToken;
        return newToken;
    }
    
    // Se falhar e a senha for diferente do default, tenta o default
    if (pass != "Password123!")
    {
        Console.WriteLine($"[AUTH-GET] Falha com senha fornecida. Tentando senha default 'Password123!'...");
        newToken = await authClient.LoginAsync(email, "Password123!");
        if (!string.IsNullOrEmpty(newToken))
        {
            userTokenCache[email] = newToken;
            return newToken;
        }
    }
    
    return null;
}

// Mapa de PropriedadeId -> OwnerEmail
var propriedadeOwnerCache = new Dictionary<Guid, string>();

async Task<string?> GetOwnerEmailForPropriedade(Guid propriedadeId)
{
    if (propriedadeOwnerCache.TryGetValue(propriedadeId, out var email))
    {
        return email;
    }

    // Se a conexão com Usuarios não estiver configurada, retornamos null
    if (string.IsNullOrWhiteSpace(options.UsuariosConnectionString) || string.IsNullOrWhiteSpace(options.PropriedadesConnectionString))
    {
        return null;
    }

    try
    {
        // 1. Obter OwnerUserId da Propriedade (DB Propriedades)
        string? ownerUserId = null;
        using (var connProp = new SqlConnection(options.PropriedadesConnectionString))
        {
            await connProp.OpenAsync();
            using var cmd = new SqlCommand("SELECT OwnerUserId FROM Propriedades WHERE Id = @Id", connProp);
            cmd.Parameters.AddWithValue("@Id", propriedadeId);
            var result = await cmd.ExecuteScalarAsync();
            if (result != null && result != DBNull.Value)
            {
                ownerUserId = result.ToString();
            }
        }

        if (string.IsNullOrWhiteSpace(ownerUserId)) return null;

        // 2. Obter Email do Usuario (DB Usuarios)
        // O OwnerUserId na tabela Propriedades pode ser o ID (int) ou algo mapeado. 
        // Vamos assumir que é o ID (int) convertido pra string, ou o Email?
        // Na estrutura do Usuario.cs o Id é int.
        
        // Tenta parsear pra int
        string queryUser = "SELECT Email FROM Usuarios WHERE Id = @Id";
        object paramValue = ownerUserId;
        
        if (int.TryParse(ownerUserId, out int userIdInt))
        {
            paramValue = userIdInt;
        }
        else 
        {
            // Se não é int, talvez seja GUID ou String. Se for email, já temos.
            if (ownerUserId.Contains("@")) 
            {
                propriedadeOwnerCache[propriedadeId] = ownerUserId;
                return ownerUserId;
            }
            // Retorna null se não soubermos lidar
            // return null; 
            // Mas vamos tentar query com string se o banco suportar conversão ou se for string
        }

        using (var connUser = new SqlConnection(options.UsuariosConnectionString))
        {
            await connUser.OpenAsync();
            using var cmdUser = new SqlCommand(queryUser, connUser);
            cmdUser.Parameters.AddWithValue("@Id", paramValue);
            var resultUser = await cmdUser.ExecuteScalarAsync();
            if (resultUser != null && resultUser != DBNull.Value)
            {
                var userEmail = resultUser.ToString();
                if (!string.IsNullOrEmpty(userEmail))
                {
                    propriedadeOwnerCache[propriedadeId] = userEmail;
                    return userEmail;
                }
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERRO-OWNER] Falha ao buscar dono da propriedade {propriedadeId}: {ex.Message}");
    }

    return null;
}

async Task<List<(Guid Id, Guid PropriedadeId)>> FetchTalhoesDinamicamente()
{
    var list = new List<(Guid, Guid)>();

    if (!string.IsNullOrWhiteSpace(options.PropriedadesConnectionString))
    {
         // Conexão via ADO.NET
         try
         {
             using var conn = new SqlConnection(options.PropriedadesConnectionString);
             await conn.OpenAsync();
             using var cmd = new SqlCommand("SELECT Id, PropriedadeId FROM Talhoes", conn);
             using var reader = await cmd.ExecuteReaderAsync();
             while (await reader.ReadAsync())
             {
                 list.Add((reader.GetGuid(0), reader.GetGuid(1)));
             }
             // Console.WriteLine($"[DB-PROP] Carregados {list.Count} talhões via SQL.");
             return list;
         }
         catch (Exception ex)
         {
             Console.WriteLine($"[ERRO-DB-PROP] Falha ao buscar talhões no banco: {options.PropriedadesConnectionString} - {ex.Message}");
             return list;
         }
    }

    try 
    {
        var resp = await propHttp.GetAsync("api/v1/Propriedades/admin/simulacao/talhoes");
        if (resp.IsSuccessStatusCode)
        {
            var content = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);
            // var list = new List<(Guid, Guid)>(); // Conflict with outer variable
            foreach (var item in doc.RootElement.EnumerateArray())
            {
                 if (item.TryGetProperty("id", out var idElem) && item.TryGetProperty("propriedadeId", out var propElem))
                 {
                     list.Add((idElem.GetGuid(), propElem.GetGuid()));
                 }
            }
            return list;
        }
        else
        {
            Console.WriteLine($"[ERRO-PROP] Falha ao buscar talhões: {resp.StatusCode}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERRO-PROP] Exceção ao buscar talhões: {ex.Message}");
    }
    return list;
}

async Task SendDataForTalhao(Guid idTalhao, SimuladorOptions currentOptions, int totalPorTalhao)
{
    // O Header Authorization já foi setado pelo caller (GetTokenForUser)
    
    for (int i = 0; i < totalPorTalhao; i++)
    {
         var leitura = LeituraSensorDto.CriarAleatoria(idTalhao, currentOptions, rnd);
         var content = new StringContent(JsonSerializer.Serialize(leitura, jsonOptions), Encoding.UTF8, "application/json");

         try
         {
             var resp = await http.PostAsync("api/v1/leituras-sensores", content);
             
             // Se autorização falhar (401), podemos tentar invalidar o cache de token para este user?
             // Como não temos o user email fácil aqui, vamos apenas logar e seguir.
             // O caller deveria tratar re-login se necessário, mas para simulação simples:
             
             if (resp.StatusCode == System.Net.HttpStatusCode.Unauthorized)
             {
                 Console.WriteLine($"[ERRO-AUTH] 401 Unauthorized para Talhão={idTalhao}. Verifique credenciais do dono.");
             }

             if (!resp.IsSuccessStatusCode)
             {
                 var body = await resp.Content.ReadAsStringAsync();
                 Console.WriteLine($"[ERRO] Talhão={idTalhao} Status={(int)resp.StatusCode} {resp.ReasonPhrase} Body={body}");
             }
             else
             {
                 // Log menos verboso
                 if (totalPorTalhao <= 1 || i % 10 == 0)
                     Console.WriteLine($"[OK] Talhão={idTalhao} ({i+1}/{totalPorTalhao}) CapturaUtc={leitura.DataHoraCapturaUtc:o} Umidade={leitura.Metricas.UmidadeSoloPercentual}%");
             }
         }
         catch (Exception ex)
         {
             Console.WriteLine($"[EXCEÇÃO] Talhão={idTalhao} {ex.GetType().Name}: {ex.Message}");
         }
    }
}

// Loop principal de simulação temporal
Console.WriteLine("Pressione ENTER para iniciar a simulação ('Start')...");
// Console.ReadLine(); // Removido para execução automática em container

var interval = TimeSpan.FromSeconds(options.IntervaloSeconds);

Console.WriteLine($"Iniciando simulação contínua com intervalo de {interval.TotalSeconds} segundo(s)...");

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (sender, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

try
{
    while (!cts.Token.IsCancellationRequested)
    {
        if (simulationUsers.Count > 0)
        {
            // Modo Massivo
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Enviando lote massivo para {simulationUsers.Count} usuarios...");
            foreach (var user in simulationUsers)
            {
                // Seta token no header
                http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.Token);

                foreach (var prop in user.Properties)
                {
                     // Configurar opções para esta propriedade
                     // Precisamos de um SimulatedOptions com o ID correto
                     var propOptions = options with { IdPropriedade = prop.Id };
                     
                     foreach (var talhaoId in prop.Talhoes)
                     {
                         await SendDataForTalhao(talhaoId, propOptions, options.TotalPorTalhao);
                     }
                }
            }
        }
        else
        {
            // Define quais talhões processar
            var talhoesToProcess = new List<(Guid Id, Guid PropriedadeId)>();

            if (options.Talhoes.Count > 0)
            {
                // Modo fixo via args/env
                foreach(var t in options.Talhoes) 
                {
                    talhoesToProcess.Add((t, options.IdPropriedade));
                }
            }
            else
            {
                // Modo dinâmico: busca da API
                var fetched = await FetchTalhoesDinamicamente();
                if (fetched.Count > 0)
                {
                    talhoesToProcess.AddRange(fetched);
                }
                else 
                {
                     // Fallback se falhar ou vazio para evitar loop vazio
                     // Console.WriteLine("[WARN] Nenhum talhão encontrado na API. Tentando novamente no próximo ciclo.");
                }
            }

            if (talhoesToProcess.Count > 0)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Enviando lote de {talhoesToProcess.Count * options.TotalPorTalhao} mensagens para a fila...");

                foreach (var item in talhoesToProcess)
                {
                    var idTalhao = item.Id;
                    var propId = item.PropriedadeId;

                    var currentOptions = options with { IdPropriedade = propId };
                    
                    // Se a variável de ambiente MULTI_PROPRIEDADE=true foi removida/substituída pelo modo dinâmico real. 
                    // Mas mantemos compatibilidade se o usuário passou args explícitos?
                    // Se args explícitos, usamos IdPropriedade fixo do options.
                    // Se dinâmico, usamos o propId que veio da api.
                    // O código original tinha lógica de randomizar propriedade se MULTI_PROPRIEDADE=true.
                    // Vamos manter se o usuário estiver usando talhões fixos?
                    // Se estamos no modo fixo (talhoesToProcess veio de options.Talhoes), propId é options.IdPropriedade.
                    
                    if (options.Talhoes.Count > 0 && Environment.GetEnvironmentVariable("MULTI_PROPRIEDADE") == "true")
                    {
                         if (rnd.NextDouble() > 0.5) 
                        {
                            var suffix = rnd.Next(2, 6); 
                            var randomProp = Guid.Parse($"00000000-0000-0000-0000-00000000000{suffix}");
                            currentOptions = options with { IdPropriedade = randomProp };
                        }
                    }
                    
                    // Resolvendo autenticação dinâmica baseada no dono da propriedade
                    string? tokenToUse = null;
                   
                    // Tenta identificar o dono
                    var ownerEmail = await GetOwnerEmailForPropriedade(propId);
                    if (!string.IsNullOrEmpty(ownerEmail))
                    {
                        var userToken = await GetTokenForUser(ownerEmail, "Password123!");
                        if (!string.IsNullOrEmpty(userToken))
                        {
                            tokenToUse = userToken;
                        }
                    }

                    // Se não conseguiu token específico, tenta usar o global/env
                    if (string.IsNullOrEmpty(tokenToUse))
                    {
                        tokenToUse = options.BearerToken;
                    }

                    // Configura header SE tiver token
                    if (!string.IsNullOrEmpty(tokenToUse))
                    {
                        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenToUse);
                    }
                    else
                    {
                        // Se não tem token nenhum, remove o header para não enviar lixo (ou envia sem auth e toma 401)
                        http.DefaultRequestHeaders.Authorization = null;
                    }
                    
                    await SendDataForTalhao(idTalhao, currentOptions, options.TotalPorTalhao);
                }
            }
        }

        await Task.Delay(interval, cts.Token);
    }
}
catch (OperationCanceledException)
{
    // Ignorar exception ao cancelar
}

Console.WriteLine("Simulação finalizada (interrompida).");
