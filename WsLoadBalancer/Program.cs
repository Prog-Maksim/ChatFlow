using System.Net;
using Microsoft.AspNetCore.HttpOverrides;
using Prometheus;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Elasticsearch;
using StackExchange.Redis;
using WsLoadBalancer.Service;

var builder = WebApplication.CreateBuilder(args);

// Настройка логирования
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(builder.Configuration.GetConnectionString("ElasticSearch") ?? throw new InvalidOperationException("`ElasticSearch` is not set in configuration.")))
    {
        AutoRegisterTemplate = true,
        IndexFormat = "logs-{0:yyyy.MM.dd}",
        FailureCallback = exception => Console.WriteLine($"Не удалось отправить лог в Elasticsearch: {exception.Exception.Message}"),
        MinimumLogEventLevel = LogEventLevel.Information
    })
    .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Hour,
        restrictedToMinimumLevel: LogEventLevel.Information)
    .WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Debug)
    .CreateLogger();

builder.Host.UseSerilog();

// Добавление сервисов
builder.Services.AddHostedService<KafkaEventConsumer>();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownProxies.Add(IPAddress.Parse("::ffff:172.17.0.1"));
});

builder.Services.AddSingleton<KafkaEventProducer>();
builder.Services.AddSingleton<BalancedService>();


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Подключаем Redis
var redisDefault = builder.Configuration.GetConnectionString("RedisDefault")
                   ?? throw new InvalidOperationException("Default Redis connection is missing.");

builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisDefault));

var app = builder.Build();

// Настройки среды
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseStatusCodePages();
}

app.UseWebSockets();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseHttpMetrics();

app.MapControllers();

// Настройка метриков
app.UseWhen(ctx => ctx.Request.Path.StartsWithSegments("/metrics"), appBuilder =>
{
    appBuilder.Use(async (context, next) =>
    {
        var username = builder.Configuration["MetricsAuth:Username"];
        var password = builder.Configuration["MetricsAuth:Password"];
        
        var headers = context.Request.Headers;
        if (!headers.ContainsKey("Authorization"))
        {
            context.Response.Headers["WWW-Authenticate"] = "Basic";
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Unauthorized");
            return;
        }

        var authHeader = headers["Authorization"].ToString();
        var encodedUsernamePassword = authHeader.Substring("Basic ".Length).Trim();
        var decodedBytes = Convert.FromBase64String(encodedUsernamePassword);
        var decoded = System.Text.Encoding.UTF8.GetString(decodedBytes);
        var parts = decoded.Split(':');
        
        if (parts.Length != 2 || parts[0] != username || parts[1] != password)
        {
            context.Response.StatusCode = 403;
            await context.Response.WriteAsync("Forbidden");
            return;
        }
        
        await next(context);
    });
    
    appBuilder.UseRouting();
    appBuilder.UseEndpoints(endpoints =>
    {
        endpoints.MapMetrics();
    });
});

app.Run();