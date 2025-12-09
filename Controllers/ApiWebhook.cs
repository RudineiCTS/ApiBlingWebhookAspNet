using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Logging;

[ApiController]
[Route("api/webhook")]
public class WebhookController : ControllerBase
{
    private readonly ILogger<WebhookController> _logger;
    private readonly WebhookService _service;

    public WebhookController(ILogger<WebhookController> logger, WebhookService service)
    {
        _logger = logger;
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> HandleWebhook()
    {
        try
        {
            // Pegando o JSON injetado pelo middleware
            var data = HttpContext.Items["jsonBody"] as JObject;

            _logger.LogInformation($"{DateTime.Now} - arquivo recebido: {data?.ToString()}");

            if (data == null)
            {
                return BadRequest(new { error = "JSON não encontrado após validação." });
            }

            // Chama o service igual no Node
            var retorno = await _service.ProcessWebhook(data);

            if (retorno.Success != true)
            {
                _logger.LogError($"Erro no processamento: {retorno}");
                return Conflict(new { Message = "Não foi possível inserir", retorno });
            }

            return StatusCode(201,retorno);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro interno ao processar webhook");
            return StatusCode(500, new { error = "Erro interno ao processar webhook" });
        }
    }
}
