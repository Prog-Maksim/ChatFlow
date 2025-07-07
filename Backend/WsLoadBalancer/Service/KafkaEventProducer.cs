using System.Text.Json;
using Confluent.Kafka;
using WsLoadBalancer.Models.Other;

// using WsLoadBalancer.Models.DB;
// using WsLoadBalancer.Models.Events;

namespace WsLoadBalancer.Service;

public class KafkaEventProducer
{
    private readonly IProducer<Null, string> _producer;
    private readonly ILogger<KafkaEventProducer> _logger;
    private const string TopicName = "message";

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
    /// Отправка события о новом сообщении
    /// </summary>
    /// <param name="messageData">Объект сообщения</param>
    /// <param name="persons">Список пользователей</param>
    /// <exception cref="ProduceException{Null, string}">
    /// Возникает при ошибке отправки сообщения в Kafka (например, проблемы с подключением или сериализацией).
    /// </exception>
    /// <exception cref="JsonException">
    /// Возникает при ошибке сериализации сообщения в JSON.
    /// </exception>
    public async Task PublishMessageAsync(MessageData messageData, List<string> persons)
    {
        var sendMessageData = new { MessageData = messageData, Persons = persons };
        var message = new Message<Null, string> { Value = JsonSerializer.Serialize(sendMessageData) };
        
        try
        {
            var deliveryResult = await _producer.ProduceAsync(TopicName, message);
            _logger.LogInformation($"Сообщение успешно доставлено{deliveryResult.TopicPartitionOffset}");
        }
        catch (ProduceException<Null, string> ex)
        {
            _logger.LogError(ex, $"Ошибка отправки сообщения: {ex.Error.Reason}");
            throw;
        }
        catch (OperationCanceledException)
        {
            _logger.LogTrace("Отправка сообщения была отменена");
        }
    }
}