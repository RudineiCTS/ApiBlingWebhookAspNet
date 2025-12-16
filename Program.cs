using Infrastructure.Logging;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// =======================================
// 1) CONFIGURAÇÃO DO SERILOG
// =======================================
builder.Host.UseSerilog((context, services, logConfig) =>
{
    logConfig
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext();
});

// =======================================
// 2) SERVICES
// =======================================
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// DI
builder.Services.AddScoped<WebhookService>();
builder.Services.AddScoped<RepositoryBlingWebhook>();

var app = builder.Build();

// =======================================
// 3) SWAGGER (SOMENTE DEV)
// =======================================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// =======================================
// 4) PIPELINE HTTP
// =======================================

// Necessário para IIS quando há proxy / SSL offloading
app.UseForwardedHeaders();

// HTTPS
app.UseHttpsRedirection();

// Log básico (status, tempo, método)
app.UseSerilogRequestLogging();

// 🔥 Middleware de LOG COMPLETO (body + erro)
app.UseMiddleware<RequestLoggingMiddleware>();

// Authorization
app.UseAuthorization();

// Middleware de segurança
app.UseWhen(
    context =>
        !context.Request.Path.StartsWithSegments("/swagger")
        && !context.Request.Path.StartsWithSegments("/favicon"),
    appBuilder =>
    {
        appBuilder.UseMiddleware<ProtectReplayMiddleware>();
    }
);

// Controllers
app.MapControllers();

app.Run();
