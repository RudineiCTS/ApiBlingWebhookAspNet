
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

public class RepositoryBlingWebhook
{
    private readonly string _connectionString;
    private readonly ILogger<RepositoryBlingWebhook> _logger;

    public RepositoryBlingWebhook(IConfiguration config, ILogger<RepositoryBlingWebhook> logger)
    {
        _logger = logger;
        _connectionString = config.GetConnectionString("SqlServer")!;
    }

    [Obsolete]
    public async Task<WebhookResponse> SalvarEvento(WebhookPayload payload)
    {
        try
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand("dbo.uspBuscaRetornoWebhooks", conn);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@INvchEventoID", payload.EventId ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@INvchTipoEvento", payload.Event ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@INvchJsonRetorno", JsonConvert.SerializeObject(payload.RawJson));

            await cmd.ExecuteNonQueryAsync();

            return new WebhookResponse
            {
                Success = true,
                Message = "Procedure executada com sucesso",
                ProcessedAt = DateTime.UtcNow.ToString("o"),
                EventId = payload.EventId
            };
        }
        catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
        {
            // erro de chave duplicada
            _logger.LogError($"Erro duplicado: {ex.Message}");

            return new WebhookResponse
            {
                Success = false,
                Message = "Registro duplicado, não foi possível inserir evento",
                ProcessedAt = DateTime.UtcNow.ToString("o"),
                EventId = payload.EventId
            };
        }
        catch (SqlException ex)
        {
            _logger.LogError($"Erro SQL: {ex.Message}");

            return new WebhookResponse
            {
                Success = false,
                Message = "Erro inesperado ao salvar webhook.",
                ProcessedAt = DateTime.UtcNow.ToString("o"),
                EventId = "Error"
            };
        }
    }
}
