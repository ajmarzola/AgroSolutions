using System.Net;
using System.Net.Http.Headers;

namespace AgroSolutions.Ingestao.WebApi.Infrastructure.Services;

public interface IPropriedadesService
{
    Task<bool> ValidateTalhaoOwnershipAsync(Guid idTalhao, string token);
}

public class PropriedadesService : IPropriedadesService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PropriedadesService> _logger;

    public PropriedadesService(HttpClient httpClient, ILogger<PropriedadesService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<bool> ValidateTalhaoOwnershipAsync(Guid idTalhao, string token)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"api/v1/Propriedades/talhoes/{idTalhao}");
            // Propagate the bearer token
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase));

            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                _logger.LogWarning("Access forbidden for Talhao {TalhaoId}", idTalhao);
                return false;
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                 _logger.LogWarning("Talhao {TalhaoId} not found", idTalhao);
                 return false;
            }
            
            _logger.LogError("Unexpected status code {StatusCode} when validating Talhao {TalhaoId}", response.StatusCode, idTalhao);
            return false;
        }
        catch (Exception ex)
        {
             _logger.LogError(ex, "Error calling Propriedades service for Talhao {TalhaoId}", idTalhao);
             return false;
        }
    }
}
