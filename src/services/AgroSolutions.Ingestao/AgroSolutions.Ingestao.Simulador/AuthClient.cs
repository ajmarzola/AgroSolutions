using System.Net.Http.Json;
using System.Text.Json;

namespace AgroSolutions.Ingestao.Simulador;

public class AuthClient
{
    private readonly HttpClient _http;
    
    public AuthClient(string baseUrl)
    {
        _http = new HttpClient { BaseAddress = new Uri(baseUrl) };
    }

    public async Task<string?> LoginAsync(string email, string password)
    {
        try 
        {
            var response = await _http.PostAsJsonAsync("api/usuarios/login", new { Email = email, Password = password });
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[AUTH-ERRO] Falha ao logar. Status: {response.StatusCode}");
                return null;
            }

            var body = await response.Content.ReadFromJsonAsync<AuthResponse>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return body?.Token;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AUTH-EX] {ex.Message}");
            return null;
        }
    }

    private class AuthResponse
    {
        public string? Token { get; set; }
    }
}
