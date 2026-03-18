using CesceSync.Config;
using CesceSync.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text.Json;

namespace CesceSync.Services;

public class AuthService
{
    private readonly HttpClient _httpClient;
    private readonly CesceApiConfig _config;
    private readonly ILogger<AuthService> _logger;

    private string? _cachedToken;
    private DateTime _tokenExpiration;

    public AuthService(
        HttpClient httpClient,
        IOptions<CesceApiConfig> config,
        ILogger<AuthService> logger)
    {
        _httpClient = httpClient;
        _config = config.Value;
        _logger = logger;
    }

    // Método para obtener token, con caching en memoria
    public async Task<string> GetTokenAsync()
    {
        // Si TokenUrl no viene, lo montamos de BaseUrl
        var tokenUrl = string.IsNullOrWhiteSpace(_config.TokenUrl)
            ? $"{_config.BaseUrl.TrimEnd('/')}/oauth2/v1/token"
            : _config.TokenUrl;

        // Si el token sigue vigente, lo devolvemos
        if (!string.IsNullOrEmpty(_cachedToken) && DateTime.Now < _tokenExpiration)
        {
            return _cachedToken;
        }

        _logger.LogInformation("Solicitando nuevo token OAuth a CESCE...");

        var request = new TokenRequest
        {
            client_id = _config.ClientId,
            client_secret = _config.ClientSecret,
            username = _config.Username,
            password = _config.Password,
            scope = _config.Scope
        };

        // Construimos el contenido del formulario para la solicitud de token
        var formContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", request.grant_type),
            new KeyValuePair<string, string>("client_id", request.client_id),
            new KeyValuePair<string, string>("client_secret", request.client_secret),
            new KeyValuePair<string, string>("scope", request.scope),
            new KeyValuePair<string, string>("username", request.username),
            new KeyValuePair<string, string>("password", request.password)
        });

        // Realizamos la solicitud POST para obtener el token
        var response = await _httpClient.PostAsync(_config.TokenUrl, formContent);

        // Verificamos si la respuesta fue exitosa
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            _logger.LogError("Error al obtener token: {error}", error);
            throw new Exception($"Error solicitando token: {response.StatusCode}");
        }

        // Leemos la respuesta y deserializamos el token
        var json = await response.Content.ReadAsStringAsync();

        // Validamos que la respuesta contenga el token
        var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(json);

        // Validamos que el token esté presente en la respuesta
        if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.access_token))
        {
            throw new Exception("La respuesta de token no contiene access_token.");
        }

        // Guardamos token en memoria
        _cachedToken = tokenResponse.access_token;
        _tokenExpiration = DateTime.Now.AddSeconds(tokenResponse.expires_in - 60);

        _logger.LogInformation("Token obtenido y cacheado. Expira a las {exp}", _tokenExpiration);

        // Devolvemos el token obtenido
        return _cachedToken;
    }
}