using Newtonsoft.Json.Linq;

public class WebhookPayload
{
    public string? EventId { get; set; }
    public string? Event { get; set; }
    public JObject? RawJson { get; set; }  // opcional para guardar tudo
}
