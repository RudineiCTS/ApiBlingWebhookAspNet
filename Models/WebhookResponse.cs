public class WebhookResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public string ProcessedAt { get; set; } = "";
    public string? EventId { get; set; }
}
