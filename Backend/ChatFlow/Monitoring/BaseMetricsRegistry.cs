using Prometheus;

namespace ChatFlow.Monitoring;

public static class MetricsRegistry
{
    // Безопасность
    
    // Брутфорс
    public static readonly Counter BruteForceDetection = Metrics.CreateCounter(
        "authservice_brute_force_attempts_total",
        "Number of detected brute-force attempts",
        new CounterConfiguration
        {
            LabelNames = [ "ip"]
        });
    
    // Счетчик активных подключений 
    public static readonly Gauge TotalActiveConnections = Metrics
        .CreateGauge("websocket_active_connections_total",
            "Общее количество активных подключений к WebSocket",
            new GaugeConfiguration());
    
    // Страны подключения пользователей
    public static readonly Gauge ActiveConnectionsByGeo = Metrics
        .CreateGauge("websocket_active_connections_by_geo",
            "Активные подключения к WebSocket по странам и городам",
            new GaugeConfiguration
            {
                LabelNames = 
                [
                    "country", 
                    "city", 
                    "Latitude", 
                    "Longitude"
                ]
            });
    
    // Создание пользователей
    public static readonly Counter UserCreationCounter = Metrics.CreateCounter(
        "users_created_total",
        "Общее количество созданных пользователей");

    // Создание чатов (по типам)
    public static readonly Counter ChatCreationCounter = Metrics.CreateCounter(
        "chats_created_total",
        "Общее количество созданных чатов по типам",
        new CounterConfiguration
        {
            LabelNames = [ "type" ]
        });

    // Отправка сообщений
    public static readonly Counter MessagesSentCounter = Metrics.CreateCounter(
        "messages_sent_total",
        "Общее количество отправленных сообщений по типам");
}