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
}