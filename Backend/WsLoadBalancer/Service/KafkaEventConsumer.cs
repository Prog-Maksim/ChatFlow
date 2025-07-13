using System.Text.Json;
using Confluent.Kafka;
using WsLoadBalancer.Models.Other;

namespace WsLoadBalancer.Service;

public class KafkaEventConsumer : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<KafkaEventConsumer> _logger;
    private readonly string _topic1 = "send-message";
    private readonly string _topic2 = "update-message";
    private readonly string _topic3 = "delete-message";
    private readonly string _groupId = "loadbalancer-service-consumer-group";
    private readonly BalancedService _balancedService;
    

    public KafkaEventConsumer(IConfiguration configuration, ILogger<KafkaEventConsumer> logger, BalancedService balancedService)
    {
        _configuration = configuration;
        _logger = logger;
        _balancedService = balancedService;
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
        consumer.Subscribe(new List<string> { _topic1, _topic2, _topic3 });
        _logger.LogInformation("KafkaEventConsumer запущен");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var result = consumer.Consume(stoppingToken);

                if (result.Topic == _topic1 || result.Topic == _topic2 || result.Topic == _topic3)
                {
                    var message = JsonSerializer.Deserialize<NewMessage>(result.Message.Value);
                    
                    if (message is null)
                    {
                        _logger.LogWarning("Получен объект null при преобразовании: {@message}", result.Message.Value);
                        continue;
                    }
                        
                    await _balancedService.AddMessageToStack(message);
                    _logger.LogInformation("Получено сообщение в топик ({result})", result.Topic);
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

public class NewMessage
{
    public required MessageData MessageData { get; set; }
    public required List<ChatUser> Persons { get; set; }
}