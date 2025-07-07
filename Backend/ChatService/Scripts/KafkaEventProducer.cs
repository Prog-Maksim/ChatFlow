using System.Text.Json;
using ChatService.Models.Events;
using Confluent.Kafka;

namespace ChatService.Scripts;

public class KafkaEventProducer
{
    private readonly IProducer<Null, string> _producer;
    private readonly ILogger<KafkaEventProducer> _logger;
    private const string TopicName = "chat.created";

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
    /// Отправка события о создании чата
    /// </summary>
    /// <param name="event">Обьект события</param>
    public async Task PublishUserCreatedAsync(ChatCreated @event)
    {
        var message = new Message<Null, string>
        {
            Value = JsonSerializer.Serialize(@event)
        };

        try
        {
            var deliveryResult = await _producer.ProduceAsync(TopicName, message);
            _logger.LogInformation($"Message delivered to {deliveryResult.TopicPartitionOffset}");
        }
        catch (ProduceException<Null, string> ex)
        {
            _logger.LogError(ex, $"Kafka produce error: {ex.Error.Reason}");
            throw;
        }
    }
}