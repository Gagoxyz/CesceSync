
using CesceSync.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Data;

namespace CesceSync.Database;

// Repositorio para acceder a datos relacionados con clientes y ejecutar el SP de sincronización
public class ClienteRepository(IConfiguration config)
{
    // La conexión a la base de datos se obtiene del appsettings.json usando la clave "DbConnection"
    private readonly string _connectionString = config.GetConnectionString("DbConnection")
            ?? throw new Exception("Parámetro DbConnection mal configurado en appsettings.");

    // Ejecuta el SP y devuelve una lista de clientes no encontrados
    public async Task<List<ClienteNoEncontrado>> EjecutarSyncSpAsync(MovimientoCesce mov, CancellationToken ct = default)
    {
        var lista = new List<ClienteNoEncontrado>();

        using var conn = new SqlConnection(_connectionString);
        using var cmd = conn.CreateCommand();

        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = "NET_SyncClientesRiesgoCESCE";

        cmd.Parameters.AddWithValue("@NIF", mov.taxCode ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@Poliza", mov.endorsementNo ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@RiesgoMaximo", mov.creditLimitRequested);
        cmd.Parameters.AddWithValue("@FechaRiesgo", mov.effectiveDate ?? DateTime.Now);
        cmd.Parameters.AddWithValue("@Estado", int.TryParse(mov.statusCode, out var code) ? code : 0);

        await conn.OpenAsync(ct);
        using var reader = await cmd.ExecuteReaderAsync(ct);

        // Lee los resultados del SP y los mapea a la lista de ClienteNoEncontrado
        while (await reader.ReadAsync(ct))
        {
            lista.Add(new ClienteNoEncontrado
            {
                NIF = reader.GetString(0),
                Poliza = reader.GetString(1),
                RiesgoMaximo = Convert.ToDecimal(reader.GetDouble(2)),
                FechaRiesgo = reader.GetDateTime(3)
            });
        }

        return lista;
    }
}