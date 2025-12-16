using System.Data;
using System.Data.SqlClient;
using Newtonsoft.Json;
using Serilog;
using ILogger = Serilog.ILogger;

public class RepositoryBlingWebhook
{
    private readonly string _connectionString;
    private readonly ILogger _logger;

    public RepositoryBlingWebhook(IConfiguration config)
    {
        _logger = Log.ForContext<RepositoryBlingWebhook>();
        _connectionString = config.GetConnectionString("SqlServer")
            ?? throw new InvalidOperationException("ConnectionString SqlServer não configurada");
    }

    public async Task<WebhookResponse> SalvarEvento(WebhookPayload payload)
    {
        try
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("dbo.uspBuscaRetornoWebhooks", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.AddWithValue("@INvchEventoID", payload.EventId ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@INvchTipoEvento", payload.Event ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue(
                "@INvchJsonRetorno",
                JsonConvert.SerializeObject(payload.RawJson)
            );

            await cmd.ExecuteNonQueryAsync();

            _logger.Information(
                "Webhook salvo com sucesso. EventId={EventId}",
                payload.EventId
            );

            return new WebhookResponse
            {
                Success = true,
                Message = "Evento salvo com sucesso",
                ProcessedAt = DateTime.UtcNow.ToString("o"),
                EventId = payload.EventId
            };
        }
        catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
        {
            _logger.Warning(
                ex,
                "Webhook duplicado ignorado. EventId={EventId}",
                payload.EventId
            );

            return new WebhookResponse
            {
                Success = false,
                Message = "Evento duplicado",
                ProcessedAt = DateTime.UtcNow.ToString("o"),
                EventId = payload.EventId
            };
        }
        catch (SqlException ex)
        {
            _logger.Error(
                ex,
                "Erro SQL ao salvar webhook. EventId={EventId}",
                payload.EventId
            );

            throw; // 🔥 deixa o Service decidir
        }
    }
}
