using CesceSync.Config;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CesceSync.Services;

public class LogFileService
{
    private readonly LoggingFilesOptions _options;
    private readonly ILogger<LogFileService> _logger;
    private readonly SemaphoreSlim _mutex = new(1, 1);

    // La ruta raíz de los logs se construye a partir de BasePath configurado en LoggingFilesOptions, añadiendo una subcarpeta "logs".
    // Si BasePath no está configurado, se usará la carpeta actual del proceso.
    private string LogsRootPath => Path.Combine(_options.BasePath ?? string.Empty, "logs");

    public LogFileService(
        IOptions<LoggingFilesOptions> options,
        ILogger<LogFileService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    // Asegura que la carpeta de logs existe, si no la crea. Lanza excepción si no se puede acceder a la ruta.
    public async Task EnsureFolderAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.BasePath))
            throw new InvalidOperationException("LoggingFiles:BasePath no está configurado.");

        try
        {
            if (!Directory.Exists(LogsRootPath))
            {
                Directory.CreateDirectory(LogsRootPath);
                _logger.LogInformation("Carpeta de logs creada en {Path}", LogsRootPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo crear la carpeta de logs en {Path}", LogsRootPath);
            throw;
        }
        await Task.CompletedTask;
    }

    // Elimina los archivos de log que sean más antiguos que el número de días configurado en RetentionDays.
    public async Task PurgeOldFilesAsync(CancellationToken ct = default)
    {
        try
        {
            if (!Directory.Exists(LogsRootPath))
                return;

            var files = Directory.GetFiles(LogsRootPath, "*.txt", SearchOption.TopDirectoryOnly);
            var limit = DateTime.UtcNow.AddDays(-_options.RetentionDays);

            foreach (var file in files)
            {
                ct.ThrowIfCancellationRequested();
                var info = new FileInfo(file);
                if (info.LastWriteTimeUtc < limit)
                {
                    try
                    {
                        info.Delete();
                        _logger.LogInformation("Log antiguo eliminado: {File}", file);
                    }
                    catch (Exception exDel)
                    {
                        _logger.LogWarning(exDel, "No se pudo eliminar el log antiguo: {File}", file);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error durante la purga de logs antiguos.");
        }
        await Task.CompletedTask;
    }

    // Guarda un mensaje de error simple en el log.
    public async Task AppendErrorAsync(string message, CancellationToken ct = default)
    {
        var line = BuildLogLine("ERROR", message);
        await AppendLineAsync(GetDailyErrorFilePath(), line, ct);
    }

    // Guarda un error con su stack trace y contexto
    public async Task AppendErrorAsync(
        Exception ex,
        string? context = null,
        string? invoiceId = null,
        bool includeStackTrace = false,
        CancellationToken ct = default)
    {
        var sb = new StringBuilder();

        sb.AppendLine(context is null ? ex.Message : $"{context} | {ex.Message}");

        if (!string.IsNullOrWhiteSpace(invoiceId))
            sb.AppendLine($"Factura: {invoiceId}");

        if (includeStackTrace && !string.IsNullOrWhiteSpace(ex.StackTrace))
            sb.AppendLine(ex.StackTrace);

        var line = BuildLogLine("ERROR", sb.ToString());
        await AppendLineAsync(GetDailyErrorFilePath(), line, ct);
    }

    // Construye una línea de log con timestamp y nivel de log. El mensaje puede contener saltos de línea.
    private string BuildLogLine(string level, string message)
    {
        var ts = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
        return $"[{ts}] [{level}] {message}{Environment.NewLine}";
    }

    // El nombre del archivo de errores incluye un prefijo configurable, la palabra "errors" y la fecha actual (ej: App-errors-20240615.txt).
    private string GetDailyErrorFilePath()
    {
        var date = DateTime.Now.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        var prefix = string.IsNullOrWhiteSpace(_options.FilePrefix) ? "App" : _options.FilePrefix;
        return Path.Combine(LogsRootPath, $"{prefix}-errors-{date}.txt");
    }

    // Método centralizado para escribir una línea en un archivo, con control de concurrencia y manejo de excepciones para evitar romper el flujo principal por problemas de I/O.
    private async Task AppendLineAsync(string filePath, string content, CancellationToken ct)
    {
        try
        {
            await _mutex.WaitAsync(ct);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            await File.AppendAllTextAsync(filePath, content, Encoding.UTF8, ct);
        }
        catch (Exception ex)
        {
            // Evitamos romper el flujo por un problema de escritura en disco
            _logger.LogWarning(ex, "No se pudo escribir en el log de fichero: {File}", filePath);
        }
        finally
        {
            _mutex.Release();
        }
    }
}
