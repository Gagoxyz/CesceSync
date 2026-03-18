using CesceSync.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Text;

namespace CesceSync.Services;

public class EnviarCorreusLauncher
{
    private readonly EnviarCorreusOptions _opt;
    private readonly ILogger<EnviarCorreusLauncher> _logger;
    private readonly LogFileService _logFileService;

    public EnviarCorreusLauncher(
        IOptions<EnviarCorreusOptions> opt,
        ILogger<EnviarCorreusLauncher> logger,
        LogFileService logFileService)
    {
        _opt = opt.Value;
        _logger = logger;
        _logFileService = logFileService;
    }

    // Ejecuta EnviarCorreus.exe con los parámetros necesarios para enviar el mail identificado por idMail.
    public async Task<EnviarCorreusResult> EjecutarAsync(Guid idMail, CancellationToken ct = default)
    {
        if (!File.Exists(_opt.ExePath))
        {
            await _logFileService.AppendErrorAsync($"No se encuentra EnviarCorreus.exe en la ruta: {_opt.ExePath}", ct);
            throw new FileNotFoundException($"No se encuentra EnviarCorreus.exe en la ruta: {_opt.ExePath}");
        }

        // El ID del mail se pasa como un GUID en formato "D" (con guiones) y en mayúsculas, para asegurar compatibilidad con lo que espera EnviarCorreus.exe.
        var idText = idMail.ToString("D").ToUpperInvariant();
        
        // Construimos los argumentos para EnviarCorreus.exe, asegurándonos de citar aquellos que puedan contener espacios.
        var args = new[]
        {
            _opt.SqlServer,
            _opt.Database,
            _opt.UserDB,
            _opt.PasswordDB,
            idText
        };

        // Configuramos el ProcessStartInfo para ejecutar EnviarCorreus.exe con los argumentos adecuados, redirigiendo la salida estándar y de error.
        var psi = new ProcessStartInfo
        {
            FileName = _opt.ExePath,
            Arguments = string.Join(" ", args.Select(a => QuoteIfNeeded(a))),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(_opt.ExePath) ?? Environment.CurrentDirectory
        };

        // Creamos un objeto para almacenar el resultado de la ejecución, incluyendo el código de salida, la salida estándar, la salida de error y si se produjo un timeout.
        var result = new EnviarCorreusResult();

        // Lanzamos el proceso y comenzamos a leer la salida estándar y de error de forma asíncrona, almacenando los resultados en StringBuilder para luego asignarlos al resultado final.
        using var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };

        // Usamos StringBuilder para acumular la salida estándar y de error, ya que pueden ser multilinea y queremos conservar todo el contenido.
        var stdOut = new StringBuilder();
        var stdErr = new StringBuilder();

        proc.OutputDataReceived += (_, e) => { if (e.Data != null) stdOut.AppendLine(e.Data); };
        proc.ErrorDataReceived += (_, e) => { if (e.Data != null) stdErr.AppendLine(e.Data); };

        _logger.LogInformation("Lanzando EnviarCorreus: {exe} {args}", psi.FileName, psi.Arguments);

        proc.Start();
        proc.BeginOutputReadLine();
        proc.BeginErrorReadLine();

        // Esperamos a que el proceso termine o a que se alcance el timeout configurado.
        // Si se alcanza el timeout, intentamos matar el proceso y marcamos el resultado como timeout.
        var timeoutMs = Math.Max(5, _opt.TimeoutSeconds) * 1000;
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var waitTask = Task.Run(() => proc.WaitForExit(timeoutMs), cts.Token);

        var finished = await waitTask;
        if (!finished)
        {
            try
            {
                result.TimedOut = true;
                _logger.LogError("Timeout esperando a EnviarCorreus (>{sec}s). Se intentará matar el proceso.", _opt.TimeoutSeconds);
                proc.Kill(entireProcessTree: true);
            }
            catch (Exception killEx)
            {
                _logger.LogError(killEx, "Error matando el proceso EnviarCorreus tras timeout.");
                await _logFileService.AppendErrorAsync($"Error matando el proceso EnviarCorreus tras timeout: {killEx.Message}", ct);
            }
        }

        // Una vez que el proceso ha terminado (ya sea por finalización normal o por timeout),
        // asignamos el código de salida y las salidas estándar y de error al resultado final, asegurándonos de recortar cualquier espacio en blanco adicional.
        result.ExitCode = proc.HasExited ? proc.ExitCode : -1;
        result.StdOut = stdOut.ToString().Trim();
        result.StdErr = stdErr.ToString().Trim();

        _logger.LogInformation("EnviarCorreus finalizado. ExitCode={code}.", result.ExitCode);
        if (!string.IsNullOrWhiteSpace(result.StdOut))
            _logger.LogDebug("EnviarCorreus STDOUT:\n{out}", result.StdOut);
        if (!string.IsNullOrWhiteSpace(result.StdErr))
            _logger.LogWarning("EnviarCorreus STDERR:\n{err}", result.StdErr);

        return result;
    }

    // Si un argumento contiene espacios o tabulaciones, lo encerramos entre comillas para que se interprete como un único argumento.
    // Si el argumento es null o vacío, lo convertimos en "" (comillas dobles) para evitar problemas de interpretación.
    private static string QuoteIfNeeded(string value)
    {
        if (string.IsNullOrEmpty(value)) return "\"\"";
        return value.Contains(' ') || value.Contains('\t') ? $"\"{value}\"" : value;
    }
}