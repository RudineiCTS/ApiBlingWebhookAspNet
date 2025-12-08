using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;

public class WebhookService
{
    private readonly ILogger<WebhookService> _logger;
    private readonly RepositoryBlingWebhook _sqlService;

    public WebhookService(ILogger<WebhookService> logger, RepositoryBlingWebhook sqlService)
    {
        _logger = logger;
        _sqlService = sqlService;
    }

    public async Task<WebhookResponse> ProcessWebhook(JObject payload)
    {
        try
        {
            var data = new WebhookPayload
            {
                EventId = payload["eventId"]?.ToString(),
                Event = payload["event"]?.ToString(),
                RawJson = payload
            };

            return await _sqlService.SalvarEvento(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro no processamento do webhook");
            throw new Exception("Falhou o processamento do webhook");
        }
    }
}
