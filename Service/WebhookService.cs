using Newtonsoft.Json.Linq;
using Serilog;
using ILogger = Serilog.ILogger;

public class WebhookService
{
    private readonly ILogger _logger;
    private readonly RepositoryBlingWebhook _repository;

    public WebhookService(RepositoryBlingWebhook repository)
    {
        _logger = Log.ForContext<WebhookService>();
        _repository = repository;
    }

    public async Task<WebhookResponse> ProcessWebhook(JObject payload)
    {
        var eventId = payload["eventId"]?.ToString();
        var eventName = payload["event"]?.ToString();

        try
        {
            var data = new WebhookPayload
            {
                EventId = eventId,
                Event = eventName,
                RawJson = payload
            };

            _logger.Information(
                "Processando webhook. Event={Event} EventId={EventId}",
                eventName,
                eventId
            );

            return await _repository.SalvarEvento(data);
        }
        catch (Exception ex)
        {
            _logger.Error(
                ex,
                "Erro ao processar webhook. Event={Event} EventId={EventId}",
                eventName,
                eventId
            );

            throw; // 🔥 mantém stack trace
        }
    }
}
