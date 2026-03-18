using CesceSync.Config;
using CesceSync.Database;
using CesceSync.Models;
using CesceSync.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CesceSync
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // Crear el host de la aplicación
            var builder = Host.CreateApplicationBuilder(args);

            // Configuración de appsettings.json
            builder.Configuration.SetBasePath(Directory.GetCurrentDirectory())
                                 .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            // Configuración específica para la API de Cesce
            builder.Services.Configure<CesceApiConfig>(builder.Configuration.GetSection("CesceApi"));

            // Registrar HttpClient para los servicios que lo necesiten
            builder.Services.AddHttpClient<AuthService>();
            builder.Services.AddHttpClient<CesceMovimientosService>();

            // Repositorios y servicios internos
            builder.Services.AddSingleton<ClienteRepository>();

            // Configuración de email
            builder.Services.Configure<EnviarCorreusOptions>(builder.Configuration.GetSection("EnviarCorreus"));

            // Repositorio y lanzador para envío de correos
            builder.Services.AddSingleton<MailQueueRepository>();
            builder.Services.AddSingleton<EnviarCorreusLauncher>();

            // Procesadores
            builder.Services.AddSingleton<MovimientoProcessor>();

            // Configuración de opciones para el servicio de logs a fichero
            builder.Services.Configure<LoggingFilesOptions>(
                builder.Configuration.GetSection("LoggingFiles"));

            // Servicio de logs a fichero
            builder.Services.AddSingleton<LogFileService>();

            // Worker
            builder.Services.AddHostedService<Worker>();

            // Construir la aplicación
            var app = builder.Build();

            // Asegurar que la carpeta de logs existe y purgar archivos antiguos al iniciar
            var fileLogger = app.Services.GetRequiredService<LogFileService>();
            await fileLogger.EnsureFolderAsync();
            await fileLogger.PurgeOldFilesAsync();

            // Ejecutar la aplicación
            app.Run();
        }
    }
}