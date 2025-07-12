using System.Text.Json;
using AuthService.Models.Events;
using Confluent.Kafka;

namespace AuthService.Scripts;

public class KafkaEventProducer
{
    private readonly ILogger<KafkaEventProducer> _logger;
    private readonly IProducer<Null, string> _producer;
    private const string TopicName = "user.created";

    public KafkaEventProducer(IConfiguration configuration, ILogger<KafkaEventProducer> logger)
    {
        var config = new ProducerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"]
        };

        _logger = logger;
        _producer = new ProducerBuilder<Null, string>(config).Build();
    }

    /// <summary>
    /// Отправка события о создании пользователя
    /// </summary>
    /// <param name="event">Обьект события</param>
    public async Task PublishUserCreatedAsync(UserCreated @event)
    {
        Random rnd = new Random();
        @event.Tag = $"@{rnd.Next(1111, 9999)}-{rnd.Next(1111, 9999)}";
        var message = new Message<Null, string>
        {
            Value = JsonSerializer.Serialize(@event)
        };

        try
        {
            var deliveryResult = await _producer.ProduceAsync(TopicName, message);
            _logger.LogDebug($"Message delivered to {deliveryResult.TopicPartitionOffset}");
        }
        catch (ProduceException<Null, string> ex)
        {
            _logger.LogError(ex, $"Kafka produce error: {ex.Error.Reason}");
            throw;
        }
    }
}