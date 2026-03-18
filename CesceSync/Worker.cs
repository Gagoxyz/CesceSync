using CesceSync.Services;

public class Worker : BackgroundService
{
    private readonly MovimientoProcessor _movimientoProcessor;
    private readonly ILogger<Worker> _logger;
    private readonly LogFileService _logFileService;
    private readonly IHostApplicationLifetime _lifetime;

    public Worker(
        MovimientoProcessor movimientoProcessor, 
        ILogger<Worker> logger,
        LogFileService logFileService,
        IHostApplicationLifetime lifetime)
    {
        _movimientoProcessor = movimientoProcessor;
        _logger = logger;
        _logFileService = logFileService;
        _lifetime = lifetime;
    }

    // El método ExecuteAsync se ejecuta una vez que el host ha iniciado. Aquí es donde se realiza el trabajo principal del servicio.
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Inicio del proceso CESCE...");

        // Procesar los movimientos de CESCE. Si ocurre un error, se registra y se detiene la aplicación.
        try
        {
            // El método ProcesarMovimientosAsync se encarga de obtener los movimientos de CESCE, procesarlos y actualizar el riesgo de los clientes asociados.
            await _movimientoProcessor.ProcesarMovimientosAsync(stoppingToken);
            _logger.LogInformation("Movimientos procesados correctamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error procesando movimientos");
            await _logFileService.AppendErrorAsync("Error procesando movimientos CESCE", ct: stoppingToken);
        }

        // Detener la aplicación después de procesar los movimientos (o en caso de error)
        _lifetime.StopApplication();
    }
}