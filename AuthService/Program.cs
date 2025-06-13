using System.Net;
using System.Reflection;
using AuthService;
using AuthService.Repository;
using AuthService.Repository.Interfaces;
using AuthService.Scripts;
using AuthService.Service;
using Elasticsearch.Net;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Prometheus;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Elasticsearch;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

var elasticSection = builder.Configuration.GetSection("ElasticSearch");
var uri = elasticSection.GetValue<string>("Uri");
var serviceToken = elasticSection.GetValue<string>("ServiceToken");

var sinkOptions = new ElasticsearchSinkOptions(new Uri(uri))
{
    AutoRegisterTemplate = true,
    IndexFormat = "logs-{0:yyyy.MM.dd}",
    FailureCallback = e => Console.WriteLine($"Не удалось отправить лог в Elasticsearch: {e.Exception.Message}"),
    MinimumLogEventLevel = LogEventLevel.Information,
    ModifyConnectionSettings = conn => conn.ApiKeyAuthentication(new ApiKeyAuthenticationCredentials(serviceToken))
};

// Настройка логирования
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Elasticsearch(sinkOptions)
    .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Hour,
        restrictedToMinimumLevel: LogEventLevel.Information)
    .WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Information)
    .CreateLogger();

builder.Host.UseSerilog();


builder.Services.AddSingleton<AuthOptions>(sp =>
    new AuthOptions(sp.GetRequiredService<IConfiguration>()));

// Добавление сервисов
builder.Services.AddScoped<IAuthRepository, AuthRepository>();

builder.Services.AddScoped<AuthService.Service.AuthService>();
builder.Services.AddScoped<AccountService>();
builder.Services.AddScoped<SessionService>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<TwoFactorService>();

builder.Services.AddSingleton<KafkaEventProducer>();
builder.Services.AddSingleton<IEncryptionService, EncryptionService>();
builder.Services.AddSingleton<TokenPublisherService>();

builder.Services.AddScoped<JwtTokenService>();

// Swagger
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
});

builder.Services.AddVersionedApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// Добавляем контроллеры
builder.Services.AddAuthorization();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options => 
{
    var authOptions = builder.Services.BuildServiceProvider().GetRequiredService<AuthOptions>();
    
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = authOptions.Issuer,
        ValidateAudience = true,
        ValidAudience = authOptions.Audience,
        ValidateLifetime = true,
        IssuerSigningKey = authOptions.GetSymmetricSecurityKey(),
        ValidateIssuerSigningKey = true
    };
    
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                context.Response.Headers.Append("Token-Expired", "true");
            return Task.CompletedTask;
        }
    };
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownProxies.Add(IPAddress.Parse("::ffff:172.17.0.1"));
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options => {
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Введите JWT токен авторизации.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "bearer"
    });
    
    options.OperationFilter<AuthorizeCheckOperationFilter>();

    var xmlFileName = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFileName));
});
builder.Services.ConfigureOptions<ConfigureSwaggerOptions>();

// Подключаем Redis
var redisDefault = builder.Configuration.GetConnectionString("RedisDefault")
                   ?? throw new InvalidOperationException("Default Redis connection is missing.");

builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisDefault));
builder.Services.AddSingleton<ISecurityRedisConnection, SecurityRedisConnection>();

// Подключает БД
string? connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationContext>(options => options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

var app = builder.Build();

// Настройки среды
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseStatusCodePages();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
        
        foreach (var description in provider.ApiVersionDescriptions)
        {
            options.SwaggerEndpoint(
                $"/swagger/{description.GroupName}/swagger.json",
                description.GroupName.ToUpperInvariant());
        }

        options.RoutePrefix = "swagger-ms1";
    });
}

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

app.MapGet("/", () => "Hello World!");

app.Run();