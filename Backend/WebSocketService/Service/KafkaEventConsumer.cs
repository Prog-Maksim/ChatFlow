using System.Text.Json;
using Confluent.Kafka;
using WebSocketService.Models.Other;
// using WebSocketService.Models.Events;
using WebSocketService.Repository.Interfaces;

namespace WebSocketService.Service;

public class KafkaEventConsumer : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<KafkaEventConsumer> _logger;
    private readonly string _topic1 = "message";
    private readonly string _topic2 = "chat.created";
    private readonly string _groupId;
    
    private readonly IWebSocketConnectionManager _webSocketConnectionManager;

    public KafkaEventConsumer(IConfiguration configuration, ILogger<KafkaEventConsumer> logger, IWebSocketConnectionManager webSocketConnectionManager)
    {
        _configuration = configuration;
        _logger = logger;
        _webSocketConnectionManager = webSocketConnectionManager;
        
        var hostname = Environment.MachineName;
        _groupId = $"websocket-service-consumer-group-{hostname}";
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
        consumer.Subscribe(new List<string> { _topic1, _topic2 });
        _logger.LogInformation("KafkaEventConsumer запущен");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var result = consumer.Consume(stoppingToken);
                _logger.LogInformation($"Получено новое сообщение в топик: {result.Topic}");
                
                if (result.Topic == _topic1)
                {
                    var message = JsonSerializer.Deserialize<NewMessage>(result.Message.Value);

                    if (message is null)
                    {
                        _logger.LogWarning("Не удалось преобразовать структуру полученную через kafka: {@message}", result.Message.Value);
                        return;
                    }

                    _logger.LogDebug("Получено сообщение: {@ex}", message);
                    foreach (var person in message.Persons)
                        await _webSocketConnectionManager.SendMessageToUserAsync(person, message.MessageData);
                }
                else if (result.Topic == _topic2)
                {
                    var message = JsonSerializer.Deserialize<ChatCreated>(result.Message.Value);

                    if (message is null)
                    {
                        _logger.LogWarning("Не удалось преобразовать структуру полученную через kafka: {@message}", result.Message.Value);
                        return;
                    }

                    _logger.LogDebug("Получено сообщение: {@ex}", message);
                    foreach (var person in message.Users)
                        await _webSocketConnectionManager.SendMessageCreateChat(message.ChatId, person.PersonId);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("KafkaEventConsumer остановлен.");
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
    public required List<string> Persons { get; set; }
}