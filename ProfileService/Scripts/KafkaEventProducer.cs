using System.Text.Json;
using ProfileService.Models.Events;
using Confluent.Kafka;

namespace ProfileService.Scripts;

public class KafkaEventProducer
{
    private readonly IProducer<Null, string> _producer;
    private const string TopicName = "user.updated";
    private readonly ILogger<KafkaEventProducer> _logger;

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
    /// Отправка события об обновлении пользователя
    /// </summary>
    /// <param name="event">Обьект события</param>
    public async Task PublishUserUpdateAsync(UserUpdated @event)
    {
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
            _logger.LogDebug($"Kafka produce error: {ex.Error.Reason}");
            throw;
        }
    }
}