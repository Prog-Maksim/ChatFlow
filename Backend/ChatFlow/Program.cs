using System.Net;
using System.Reflection;
using ChatFlow;
using ChatFlow.Models.DB;
using ChatFlow.Repository;
using ChatFlow.Repository.Interfaces;
using ChatFlow.Scripts;
using ChatFlow.Service;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;
using Nest;
using Prometheus;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Elasticsearch;
using StackExchange.Redis;
using ServerVersion = Microsoft.EntityFrameworkCore.ServerVersion;

var builder = WebApplication.CreateBuilder(args);

// Настройка логирования
var elasticSection = builder.Configuration.GetSection("ElasticSearch");
var uri = elasticSection.GetValue<string>("Uri");

if (uri is null)
    throw new NullReferenceException("ElasticSearch uri is null");

var sinkOptions = new ElasticsearchSinkOptions(new Uri(uri))
{
    AutoRegisterTemplate = false,
    IndexFormat = "logs-{0:yyyy.MM.dd}",
    FailureCallback = e => Console.WriteLine($"Не удалось отправить лог в Elasticsearch: {e.Exception?.Message}"),
    MinimumLogEventLevel = LogEventLevel.Information,
    CustomFormatter = new JsonFormatter(),
    EmitEventFailure = EmitEventFailureHandling.WriteToSelfLog
};

// Настройка логирования
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .Enrich.FromLogContext()
    .WriteTo.Elasticsearch(sinkOptions)
    .WriteTo.Console
    (
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [RequestId: {RequestId}] {Message:lj}{NewLine}{Exception}",
        restrictedToMinimumLevel: LogEventLevel.Debug)
    .CreateLogger();

builder.Host.UseSerilog();

// Добавление сервисов
builder.Services.Configure<AuthOptions>(
    builder.Configuration.GetSection("Auth"));

builder.Services.AddScoped<AccountService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ChatService>();
builder.Services.AddScoped<ImageService>();
builder.Services.AddScoped<MessageService>();
builder.Services.AddScoped<ProfileService>();
builder.Services.AddScoped<S3Service>();
builder.Services.AddScoped<SessionService>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<TwoFactorService>();
builder.Services.AddScoped<WebSocketService>();
builder.Services.AddScoped<SearchService>();

builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<ISearchRepository, SearchRepository>();
builder.Services.AddScoped<IChatRepository, ChatRepository>();
builder.Services.AddScoped<IMessageRepository, MessageRepository>();
builder.Services.AddScoped<IProfileRepository, ProfileRepository>();
builder.Services.AddSingleton<IWebSocketConnectionManager, WebSocketConnectionManager>();

builder.Services.AddScoped<TokenValidator>();

builder.Services.AddSingleton<S3Service>();
builder.Services.AddSingleton<IEncryptionService, EncryptionService>();
builder.Services.AddScoped<JwtTokenService>();

// ElasticSearch
var settings = new ConnectionSettings(new Uri(uri))
    .DefaultIndex("chats")
    .DefaultMappingFor<IndexPerson>(m => m
        .PropertyName(p => p.ChatId, "chatId")
        .PropertyName(p => p.Type, "type")
        .PropertyName(p => p.Title, "title")
        .PropertyName(p => p.Name, "name")
        .PropertyName(p => p.Surname, "surname")
        .PropertyName(p => p.Tag, "tag")
    )
    .DisableDirectStreaming()
    .PrettyJson()
    .OnRequestCompleted(details =>
    {
        Console.WriteLine(details.DebugInformation);
    })
    .DefaultFieldNameInferrer(p => p);

var elasticClient = new ElasticClient(settings);
builder.Services.AddSingleton<IElasticClient>(elasticClient);

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
    var authOptions = builder.Configuration.GetSection("Auth").Get<AuthOptions>()!;
    
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

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost5173", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod(); // разрешить все методы
    });
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

// Подключаем MongoDB
var mongoDbConnectionString = builder.Configuration.GetConnectionString("MongoDBConnection");
builder.Services.AddSingleton(new MongoClient(mongoDbConnectionString));
builder.Services.AddSingleton<IMongoClient>(new MongoClient(mongoDbConnectionString));

// Подключаем Redis
var redisDefault = builder.Configuration.GetConnectionString("RedisDefault")
                   ?? throw new InvalidOperationException("Default Redis connection is missing.");

builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisDefault));

// Подключает БД
string? connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationContext>(options => options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));


var app = builder.Build();

app.UseMiddleware<RequestIdMiddleware>();

app.UseCors("AllowLocalhost5173");

// Настройки среды
if (app.Environment.IsDevelopment())
    app.UseDeveloperExceptionPage();

app.UseWebSockets();

app.UseStatusCodePages();
app.UseSwagger(c =>
{
    c.RouteTemplate = "swagger/{documentName}/swagger.json";
});
app.UseSwaggerUI(options =>
{
    var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
    foreach (var description in provider.ApiVersionDescriptions)
    {
        options.SwaggerEndpoint(
            $"/swagger/{description.GroupName}/swagger.json",
            description.GroupName.ToUpperInvariant());
    }
    options.RoutePrefix = "swagger";
});

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
    appBuilder.UseAuthorization();
    appBuilder.UseEndpoints(endpoints =>
    {
        endpoints.MapMetrics();
    });
});

app.MapGet("/", () => "Hello World!");

app.Run();