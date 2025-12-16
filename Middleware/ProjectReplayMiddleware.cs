using System.Security.Cryptography;
using System.Text;
using Serilog;
using Newtonsoft.Json.Linq;
using ILogger = Serilog.ILogger;

public class ProtectReplayMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger _logger;
    private readonly string _blingSecret;

    public ProtectReplayMiddleware(
        RequestDelegate next,
        IConfiguration configuration)
    {
        _next = next;
        _logger = Log.ForContext<ProtectReplayMiddleware>();
        _blingSecret = configuration["BLING_CLIENT_SECRET"]
            ?? throw new InvalidOperationException("BLING_CLIENT_SECRET não configurado");
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Body já foi lido pelo RequestLoggingMiddleware
        context.Request.Body.Seek(0, SeekOrigin.Begin);

        using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
        var rawBody = await reader.ReadToEndAsync();
        context.Request.Body.Seek(0, SeekOrigin.Begin);

        var signature = context.Request.Headers["X-Bling-Signature-256"].ToString();

        if (!VerifyBlingSignature(rawBody, signature))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Assinatura inválida");
            return;
        }

        JObject jsonBody;
        try
        {
            jsonBody = JObject.Parse(rawBody);
        }
        catch
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("Body inválido");
            return;
        }

        var id = jsonBody["id"]?.ToString() ?? jsonBody["eventId"]?.ToString();
        if (string.IsNullOrWhiteSpace(id))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("Nenhum ID único encontrado");
            return;
        }

        context.Items["jsonBody"] = jsonBody;

        await _next(context);
    }

    private bool VerifyBlingSignature(string rawBody, string? headerSignature)
    {
        if (string.IsNullOrWhiteSpace(headerSignature) || !headerSignature.StartsWith("sha256="))
            return false;

        var receivedHash = headerSignature["sha256=".Length..];

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_blingSecret));
        var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawBody));

        var receivedBytes = Convert.FromHexString(receivedHash);

        return CryptographicOperations.FixedTimeEquals(computedHash, receivedBytes);
    }
}
