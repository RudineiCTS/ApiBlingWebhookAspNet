using Serilog;

var builder = WebApplication.CreateBuilder(args);

// 1) CONFIGURAÇÃO DO SERILOG
builder.Host.UseSerilog((context, logConfig) =>
{
    logConfig.ReadFrom.Configuration(context.Configuration);
});

// 2) SERVICES
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<WebhookService>();
builder.Services.AddScoped<RepositoryBlingWebhook>();

var app = builder.Build();

// 3) SWAGGER (APENAS EM DEV)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 4) HTTPS REDIRECTION (IMPORTANTE!)
app.UseHttpsRedirection();

// 5) LOG DAS REQUISIÇÕES
app.UseSerilogRequestLogging();

// 6) AUTHORIZATION
app.UseAuthorization();

// 7) SEU MIDDLEWARE CUSTOMIZADO
app.UseWhen(
    context =>
        !context.Request.Path.StartsWithSegments("/swagger")
        && !context.Request.Path.StartsWithSegments("/favicon"),
    appBuilder =>
    {
        appBuilder.UseMiddleware<ProtectReplayMiddleware>();
    }
);

// 8) CONTROLLERS
app.MapControllers();

app.Run();