using CesceSync.Database;
using CesceSync.Models;
using Microsoft.Extensions.Options;
using System.Text;

namespace CesceSync.Services;

public class MovimientoProcessor
{
    private readonly CesceMovimientosService _cesceMovimientos;
    private readonly ClienteRepository _clienteRepo;
    private readonly ILogger<MovimientoProcessor> _logger;
    private readonly List<ClienteNoEncontrado> _clientesNoEncontrados = new();
    private readonly MailQueueRepository _mailRepo;
    private readonly EnviarCorreusLauncher _enviarCorreus;
    private readonly EnviarCorreusOptions _mailOpt;
    private readonly LogFileService _logFileService;

    public MovimientoProcessor(
        CesceMovimientosService cesceMovimientos,
        ClienteRepository clienteRepo,
        ILogger<MovimientoProcessor> logger,
        MailQueueRepository mailRepo,
        EnviarCorreusLauncher enviarCorreus,
        IOptions<EnviarCorreusOptions> mailOptions,
        LogFileService logFileService
        )
    {
        _cesceMovimientos = cesceMovimientos;
        _clienteRepo = clienteRepo;
        _logger = logger;
        _enviarCorreus = enviarCorreus;
        _mailRepo = mailRepo;
        _mailOpt = mailOptions.Value;
        _logFileService = logFileService;
    }

    // Método principal: obtiene los movimientos, los procesa, y si hay clientes no encontrados, envía el mail
    public async Task ProcesarMovimientosAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Inicio procesamiento de movimientos CESCE");
   
        List<MovimientoCesce> movimientos;
        try
        {
            movimientos = await _cesceMovimientos.ObtenerMovimientosAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo movimientos de CESCE");
            await _logFileService.AppendErrorAsync("Error obteniendo movimientos de CESCE: " + ex.Message, ct);
            throw;
        }

        _logger.LogInformation("Total movimientos recibidos: {count}", movimientos.Count);

        // Procesar cada movimiento secuencialmente. Podríamos paralelizar, pero así es más sencillo manejar el orden y los logs.
        foreach (var mov in movimientos)
        {
            if (ct.IsCancellationRequested) break;

            try
            {
                await ProcesarMovimientoAsync(mov, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando movimiento endorsementNo={endorsementNo}, contractNo={contractNo}",
                    mov.endorsementNo, mov.contractNo);
                await _logFileService.AppendErrorAsync(
                    $"Error procesando movimiento endorsementNo={mov.endorsementNo}, contractNo={mov.contractNo}: {ex.Message}", ct);
            }
        }

        _logger.LogInformation("Fin procesamiento de movimientos CESCE");

        // Si se han encontrado clientes no encontrados, componer el mail, insertarlo en la cola, y lanzar EnviarCorreus
        if (_clientesNoEncontrados.Count != 0)
        {
            // 1) Componer asunto y cuerpo
            var asunto = $"{DateTime.Now.ToShortDateString()} {_mailOpt.DefaultAsunto}";
            var cuerpo = GenerarTextoClientes(_clientesNoEncontrados);

            // 2) Insertar el registro en Registro_Mails (SP)
            var destino = _mailOpt.DefaultPara;
            var quien = _mailOpt.DefaultQuien;

            var idMail = await _mailRepo.InsertarMailAsync(
                destino: destino,
                asunto: asunto,
                cuerpo: cuerpo,
                quien: quien,
                cuando: DateTime.Now,
                ct);

            // 3) Lanzar EnviarCorreus
            var res = await _enviarCorreus.EjecutarAsync(idMail, ct);

            if (res.TimedOut)
            {
                _logger.LogError("EnviarCorreus timeout para IdMail={id}", idMail);
                await _logFileService.AppendErrorAsync($"EnviarCorreus timeout para IdMail={idMail}", ct);
            }

            if (res.ExitCode != 0)
            {
                _logger.LogError("EnviarCorreus devolvió ExitCode={code} para IdMail={id}. STDOUT: {out}. STDERR: {err}",
                    res.ExitCode, idMail, res.StdOut, res.StdErr);
                await _logFileService.AppendErrorAsync("EnviarCorreus error para IdMail=" + idMail + $". ExitCode={res.ExitCode}. STDOUT: {res.StdOut}. STDERR: {res.StdErr}", ct);
            }
            else
            {
                _logger.LogInformation("EnviarCorreus OK para IdMail={id}", idMail);
            }
        }


    }

    // Procesa un único movimiento: intenta asociarlo a un cliente y actualizar su riesgo, o registra el no encontrado
    private async Task ProcesarMovimientoAsync(MovimientoCesce mov, CancellationToken ct)
    {
        var resultado = await _clienteRepo.EjecutarSyncSpAsync(mov, ct);

        if (resultado.Count != 0)
        {
            _clientesNoEncontrados.AddRange(resultado);
        }
    }

    // Genera el texto del mail a partir de la lista de clientes no encontrados
    private string GenerarTextoClientes(List<ClienteNoEncontrado> clientes)
    {
        var sb = new StringBuilder();

        sb.AppendLine("Se han detectado clientes no encontrados durante la sincronización CESCE:");
        sb.AppendLine();

        foreach (var c in clientes)
        {
            sb.AppendLine($"NIF: {c.NIF}");
            sb.AppendLine($"Póliza: {c.Poliza}");
            sb.AppendLine($"Riesgo máximo: {c.RiesgoMaximo}");
            sb.AppendLine($"Fecha riesgo: {c.FechaRiesgo:yyyy-MM-dd}");
            sb.AppendLine();
        }

        sb.AppendLine("Por favor revise estos registros.");

        return sb.ToString();
    }
}