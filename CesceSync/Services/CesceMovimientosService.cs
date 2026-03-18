using CesceSync.Config;
using CesceSync.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;

namespace CesceSync.Services;

public class CesceMovimientosService
{
    private readonly HttpClient _httpClient;
    private readonly AuthService _authService;
    private readonly CesceApiConfig _config;
    private readonly ILogger<CesceMovimientosService> _logger;
    private readonly IConfiguration _appConfig;
    private readonly LogFileService _logFileService;

    public CesceMovimientosService(
        HttpClient httpClient,
        AuthService authService,
        IOptions<CesceApiConfig> config,
        IConfiguration appConfig,
        ILogger<CesceMovimientosService> logger,
        LogFileService logFileService)
    {
        _httpClient = httpClient;
        _authService = authService;
        _config = config.Value;
        _appConfig = appConfig;
        _logger = logger;
        _logFileService = logFileService;
    }

    // Obtiene todos los movimientos de un contrato, paginando internamente
    public async Task<List<MovimientoCesce>> ObtenerMovimientosAsync(CancellationToken ct = default)
    {
        var contractNo = _appConfig["Parametros:ContractNo"];
        if (string.IsNullOrWhiteSpace(contractNo))
        {
            await _logFileService.AppendErrorAsync("Falta configurar Parametros:ContractNo en appsettings.json", ct);
            throw new Exception("Falta configurar Parametros:ContractNo");
        }

        var acumulado = new List<MovimientoCesce>();
        string? next = null;

        do
        {
            // Obtener página
            var page = await ObtenerPaginaAsync(contractNo, _config.LanguageCode, next, ct);

            // Validar error
            if (page.error?.errorCode != "0")
            {
                // La API devuelve errorCode="0" aunque haya un error, pero por si acaso...
                var desc = page.error?.errorDescription ?? "(sin descripción)";
                await _logFileService.AppendErrorAsync($"CESCE error {page.error?.errorCode}: {desc}", ct);
                throw new Exception($"CESCE error {page.error?.errorCode}: {desc}");
            }

            // Mapear y acumular movimientos
            if (page.debtor != null)
            {
                // La API devuelve debtor=null en la última página, aunque lo correcto sería devolver un array vacío. Por eso se valida antes de iterar.
                foreach (var d in page.debtor)
                {
                    // Mapear DTO -> Modelo de negocio
                    acumulado.Add(Mapear(d, page.client?.contractNo));
                }
            }
            // Preparar siguiente página
            next = page.client?.nextEndorsementNo;
            _logger.LogInformation("nextEndorsementNo={next}", next);

            // La API devuelve nextEndorsementNo="0" cuando no hay más páginas, por eso se valida que sea distinto de "0".
        } while (!string.IsNullOrWhiteSpace(next) && next != "0");

        return acumulado;
    }

    // Obtiene una página de movimientos, según el número de endorsementNo
    private async Task<MovementsEnvelope> ObtenerPaginaAsync(
        string contractNo, string languageCode, string? nextEndorsementNo, CancellationToken ct)
    {
        var token = await _authService.GetTokenAsync();

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var baseUrl = _config.BaseUrl.TrimEnd('/');
        var url = $"{baseUrl}/portfolio-movements/v1/contracts/{contractNo}/movements?languageCode={languageCode}";

        if (!string.IsNullOrWhiteSpace(nextEndorsementNo) && nextEndorsementNo != "0")
            url += $"&nextEndorsementNo={nextEndorsementNo}";

        _logger.LogInformation("GET {url}", url);

        using var res = await _httpClient.GetAsync(url, ct);
        var json = await res.Content.ReadAsStringAsync(ct);

        if (!res.IsSuccessStatusCode)
        {
            _logger.LogError("Movimientos HTTP {code}: {body}", res.StatusCode, json);
            await _logFileService.AppendErrorAsync($"Error HTTP CESCE movimientos: {res.StatusCode} - {json}", ct);
            throw new Exception($"Error HTTP CESCE movimientos: {res.StatusCode}");
        }

        var env = JsonSerializer.Deserialize<MovementsEnvelope>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (env == null)
        {
            await _logFileService.AppendErrorAsync("Respuesta CESCE movimientos no se pudo deserializar.", ct);
            throw new Exception("Respuesta CESCE vacía.");
        }
        return env;
    }

    // Mapeo DTO -> Modelo de negocio
    private static MovimientoCesce Mapear(DebtorDto d, string contractNo)
    {
        var es = new CultureInfo("es-ES");

        decimal? TryDec(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            if (s == "0" || s == "0,00") return 0m;
            if (decimal.TryParse(s, NumberStyles.Number, es, out var val))
                return val;
            return null;
        }

        DateTime? TryDate(string? s)
        {
            if (string.IsNullOrWhiteSpace(s) || s == "0") return null;
            if (DateTime.TryParseExact(s, "yyyyMMdd", CultureInfo.InvariantCulture,
                                       DateTimeStyles.None, out var dt))
                return dt;
            return null;
        }

        return new MovimientoCesce
        {
            contractNo = contractNo ?? "",
            endorsementNo = d.endorsementNo,
            statusCode = d.statusCode,
            taxCode = d.taxCode,
            creditLimitRequested = TryDec(d.creditLimitRequested) ?? 0m,
            creditLimitGranted = TryDec(d.creditLimitGranted),
            effectiveDate = TryDate(d.effectiveDate),
            validityDate = TryDate(d.validityDate),
            cancellationDate = TryDate(d.cancellationDate),
            currencyCode = string.IsNullOrWhiteSpace(d.currencyCode) ? "EUR" : d.currencyCode
        };
    }
}