using System.Text;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Infrastructure.Logging
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;

        public RequestLoggingMiddleware(RequestDelegate next)
        {
            _next = next;
            _logger = Log.ForContext<RequestLoggingMiddleware>();
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // =======================================
            // 1) REQUEST
            // =======================================
            context.Request.EnableBuffering();

            string requestBody = await ReadRequestBodyAsync(context.Request);

            var originalResponseBody = context.Response.Body;
            await using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            try
            {
                // Continua o pipeline
                await _next(context);

                // =======================================
                // 2) RESPONSE
                // =======================================
                string responseBodyText = await ReadResponseBodyAsync(responseBody);

                _logger.Information(
                    "HTTP {Method} {Path} | Status: {StatusCode} | Request: {RequestBody} | Response: {ResponseBody}",
                    context.Request.Method,
                    context.Request.Path,
                    context.Response.StatusCode,
                    requestBody,
                    responseBodyText
                );
            }
            catch (Exception ex)
            {
                _logger.Error(
                    ex,
                    "HTTP {Method} {Path} | Request: {RequestBody}",
                    context.Request.Method,
                    context.Request.Path,
                    requestBody
                );

                throw; // Repropaga a exceção
            }
            finally
            {
                // Copia a resposta de volta para o cliente
                responseBody.Seek(0, SeekOrigin.Begin);
                await responseBody.CopyToAsync(originalResponseBody);
                context.Response.Body = originalResponseBody;
            }
        }

        // =======================================
        // Helpers
        // =======================================

        private static async Task<string> ReadRequestBodyAsync(HttpRequest request)
        {
            request.Body.Seek(0, SeekOrigin.Begin);

            using var reader = new StreamReader(
                request.Body,
                Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                leaveOpen: true
            );

            string body = await reader.ReadToEndAsync();
            request.Body.Seek(0, SeekOrigin.Begin);

            return body;
        }

        private static async Task<string> ReadResponseBodyAsync(Stream responseBody)
        {
            responseBody.Seek(0, SeekOrigin.Begin);

            using var reader = new StreamReader(responseBody, Encoding.UTF8);
            return await reader.ReadToEndAsync();
        }
    }
}
