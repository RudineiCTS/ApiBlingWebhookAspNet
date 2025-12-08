using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq; // instalar package Newtonsoft.Json
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;


public class ProtectReplayMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ProtectReplayMiddleware> _logger;
    private readonly string _blingSecret;

    public ProtectReplayMiddleware(
        RequestDelegate next,
        ILogger<ProtectReplayMiddleware> logger,
        IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        _blingSecret = configuration["BLING_CLIENT_SECRET"]!;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Permitir leitura múltipla do body
        context.Request.EnableBuffering();

        using var reader = new MemoryStream();
        await context.Request.Body.CopyToAsync(reader);
        var rawBytes = reader.ToArray();

        context.Request.Body.Position = 0;

        var signature = context.Request.Headers["X-Bling-Signature-256"].ToString();

        _logger.LogInformation($"Signature recebida: {signature}");
        _logger.LogInformation($"Raw body: {Encoding.UTF8.GetString(rawBytes)}");

        // 1. VALIDAR HASH
        bool isValid = VerifyBlingSignature(rawBytes, signature, _blingSecret);
        if (!isValid)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Assinatura inválida");
            return;
        }

        // 2. PARSE DO JSON
        JObject jsonBody;
        try
        {
            jsonBody = JObject.Parse(Encoding.UTF8.GetString(rawBytes));
        }
        catch
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("Body inválido");
            return;
        }

        // 3. EXTRAI ID ÚNICO
        var id = jsonBody["id"]?.ToString() ?? jsonBody["eventId"]?.ToString();
        if (string.IsNullOrEmpty(id))
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("Nenhum ID único encontrado.");
            return;
        }

        // 4. INJETAR NO CONTROLLER (HttpContext.Items)
        context.Items["jsonBody"] = jsonBody;

        // seguir adiante
        await _next(context);
    }

    private bool VerifyBlingSignature(byte[] rawBody, string? headerSignature, string clientSecret)
    {
        if (string.IsNullOrWhiteSpace(headerSignature) || !headerSignature.StartsWith("sha256="))
            return false;

        // remove o prefixo "sha256="
        var receivedHash = headerSignature.Replace("sha256=", "");

        // gerar hash HMAC SHA256 com a mesma lógica do Node
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(clientSecret));
        var hashBytes = hmac.ComputeHash(rawBody);
        var generatedHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

        // comparar hash recebido com hash gerado
        return generatedHash == receivedHash;
    }
}
