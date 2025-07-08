using System.Text.Json;
using Confluent.Kafka;
using ProfileService.Models.Events;
using ProfileService.Repository;
using ProfileService.Repository.Interfaces;

namespace ProfileService.Scripts;

public class KafkaEventConsumer : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<KafkaEventConsumer> _logger;
    private readonly string _topic = "user.created";
    private readonly string _groupId = "profile-service-consumer-group";
    
    private readonly IServiceScopeFactory _scopeFactory;

    public KafkaEventConsumer(IConfiguration configuration, ILogger<KafkaEventConsumer> logger, IServiceScopeFactory scopeFactory)
    {
        _configuration = configuration;
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        var config = new ConsumerConfig
        {
            BootstrapServers = _configuration["Kafka:BootstrapServers"],
            GroupId = _groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest
        };

        using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();
        consumer.Subscribe(_topic);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var result = consumer.Consume(stoppingToken);
                var userCreatedEvent = JsonSerializer.Deserialize<UserUpdated>(result.Message.Value);

                if (userCreatedEvent == null)
                {
                    _logger.LogError("Получено сообщение от kafka.\nНевозможно создать пользователя. \nСтруктура: \n\n{@result}", result.Message.Value);
                    continue;
                }
                
                _logger.LogDebug("[Kafka] Получен новый пользователь: {@user}", userCreatedEvent);

                using (var scope = _scopeFactory.CreateScope())
                {
                    var repository = scope.ServiceProvider.GetRequiredService<IProfileRepository>();
                    await repository.CreatePersonAsync(userCreatedEvent);
                    await repository.SaveChangesAsync();
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("KafkaEventConsumer остановлен.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении сообщения из Kafka. \n{@error}", ex);
        }
        finally
        {
            consumer.Close();
        }
    }
}