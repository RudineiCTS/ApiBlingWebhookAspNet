using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------
// 1) CONFIGURAÇÃO DO SERILOG
// ---------------------------------------------------
builder.Host.UseSerilog((context, logConfig) =>
{
    logConfig.ReadFrom.Configuration(context.Configuration);
});

// ---------------------------------------------------
// 2) SERVICES
// ---------------------------------------------------
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// seus serviços
builder.Services.AddScoped<WebhookService>();
builder.Services.AddScoped<RepositoryBlingWebhook>();

var app = builder.Build();

// ---------------------------------------------------
// 3) MIDDLEWARE DO SERILOG
// ---------------------------------------------------
app.UseSerilogRequestLogging();

// ---------------------------------------------------
// 4) SWAGGER (não deve passar pelo middleware)
// ---------------------------------------------------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();

// ---------------------------------------------------
// 5) PROTECT REPLAY – só para rotas da API
// ---------------------------------------------------
app.UseWhen(
    context =>
        !context.Request.Path.StartsWithSegments("/swagger")
        && !context.Request.Path.StartsWithSegments("/favicon"),
    appBuilder =>
    {
        appBuilder.UseMiddleware<ProtectReplayMiddleware>();
    }
);

// ---------------------------------------------------
app.MapControllers();
// ---------------------------------------------------

app.Run();
