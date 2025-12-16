using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Serilog;
using ILogger = Serilog.ILogger;

[ApiController]
[Route("api/webhook")]
public class WebhookController : ControllerBase
{
    private readonly ILogger _logger;
    private readonly WebhookService _service;

    public WebhookController(WebhookService service)
    {
        _logger = Log.ForContext<WebhookController>();
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> HandleWebhook()
    {
        var data = HttpContext.Items["jsonBody"] as JObject;

        if (data == null)
        {
            _logger.Warning("Webhook chamado sem JSON válido após middleware");
            return BadRequest(new { error = "JSON não encontrado após validação." });
        }

        try
        {
            var result = await _service.ProcessWebhook(data);

            if (!result.Success)
            {
                _logger.Warning(
                    "Falha no processamento do webhook. Id={Id}",
                    data["id"]?.ToString() ?? data["eventId"]?.ToString()
                );

                return Conflict(new { message = "Não foi possível processar", result });
            }

            _logger.Information(
                "Webhook processado com sucesso. Id={Id}",
                data["id"]?.ToString() ?? data["eventId"]?.ToString()
            );

            return Ok(result); // ou Accepted()
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Erro interno ao processar webhook");
            return StatusCode(500, new { error = "Erro interno ao processar webhook" });
        }
    }
}
