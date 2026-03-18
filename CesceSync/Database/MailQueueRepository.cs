using CesceSync.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Data;

namespace CesceSync.Database;

public class MailQueueRepository
{
    private readonly string _connectionString;
    private readonly ILogger<MailQueueRepository> _logger;
    private readonly EnviarCorreusOptions _mailOpt;

    public MailQueueRepository(
        IConfiguration config,
        ILogger<MailQueueRepository> logger,
        IOptions<EnviarCorreusOptions> mailOptions)
    {
        _connectionString = config.GetConnectionString("DbConnection")
            ?? throw new Exception("Parámetro DbConnection mal configurado en appsettings.");
        _logger = logger;
        _mailOpt = mailOptions.Value;
    }

    // Inserta un nuevo mail en la cola de envío. Devuelve el Id generado (Guid).
    public async Task<Guid> InsertarMailAsync(
        string destino,
        string asunto,
        string cuerpo,
        short quien = 999,
        DateTime? cuando = null,
        CancellationToken ct = default)
    {
        var id = Guid.NewGuid();
        var idText = id.ToString("D").ToUpperInvariant();

        using var conn = new SqlConnection(_connectionString);
        using var cmd = conn.CreateCommand();

        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = "NET_SyncInsertarMailCESCE";

        cmd.Parameters.Add(new SqlParameter("@IdMail", SqlDbType.NVarChar, 40) { Value = idText });
        cmd.Parameters.Add(new SqlParameter("@Servidor", SqlDbType.NVarChar, 50) { Value = _mailOpt.Servidor });
        cmd.Parameters.Add(new SqlParameter("@Puerto", SqlDbType.SmallInt) { Value = _mailOpt.Puerto });
        cmd.Parameters.Add(new SqlParameter("@SSL", SqlDbType.Bit) { Value = _mailOpt.UseSSL });
        cmd.Parameters.Add(new SqlParameter("@MailOrigen", SqlDbType.NVarChar, 50) { Value = _mailOpt.MailOrigen });
        cmd.Parameters.Add(new SqlParameter("@Usuario", SqlDbType.NVarChar, 50) { Value = _mailOpt.Usuario });
        cmd.Parameters.Add(new SqlParameter("@Pass", SqlDbType.NVarChar, 50) { Value = _mailOpt.Pass });
        cmd.Parameters.Add(new SqlParameter("@Destino", SqlDbType.NVarChar) { Value = (object)destino ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@Asunto", SqlDbType.NVarChar) { Value = (object)asunto ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@Cuerpo", SqlDbType.NVarChar) { Value = (object)cuerpo ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@Quien", SqlDbType.SmallInt) { Value = quien });
        cmd.Parameters.Add(new SqlParameter("@Cuando", SqlDbType.DateTime) { Value = cuando ?? DateTime.Now });

        _logger.LogInformation("Insertando correo en Registro_Mails IdMail={id}", idText);

        await conn.OpenAsync(ct);
        await cmd.ExecuteNonQueryAsync(ct);

        return id;
    }
}