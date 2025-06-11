namespace WsLoadBalancer.Service;

public class BalancedService
{
    private readonly ILogger<BalancedService> _logger;
    private readonly KafkaEventProducer _producer;

    public BalancedService(ILogger<BalancedService> logger, KafkaEventProducer producer)
    {
        _logger = logger;
        _producer = producer;
    }
    
    /// <summary>
    /// Добавления сообщения в стек балансировки нагрузки
    /// </summary>
    /// <param name="message">Объект сообщения</param>
    public async Task AddMessageToStack(NewMessage message)
    {
        if (message.Persons.Count <= 200)
            await _producer.PublishMessageAsync(message.MessageData, message.Persons.Select(p => p.PersonId).ToList());
    }
}