using System.Text.Json;
using AuthService.Models.Events;
using Confluent.Kafka;

namespace AuthService.Scripts;

public class KafkaEventProducer
{
    private readonly IProducer<Null, string> _producer;
    private const string TopicName = "user.created";

    public KafkaEventProducer(IConfiguration configuration)
    {
        var config = new ProducerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"]
        };

        _producer = new ProducerBuilder<Null, string>(config).Build();
    }

    /// <summary>
    /// Отправка события о создании пользователя
    /// </summary>
    /// <param name="event">Обьект события</param>
    public async Task PublishUserCreatedAsync(UserCreated @event)
    {
        var message = new Message<Null, string>
        {
            Value = JsonSerializer.Serialize(@event)
        };

        try
        {
            var deliveryResult = await _producer.ProduceAsync(TopicName, message);
            Console.WriteLine($"Message delivered to {deliveryResult.TopicPartitionOffset}");
        }
        catch (ProduceException<Null, string> ex)
        {
            Console.WriteLine($"Kafka produce error: {ex.Error.Reason}");
            throw;
        }
    }
}