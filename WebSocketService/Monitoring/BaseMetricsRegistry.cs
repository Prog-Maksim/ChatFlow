using Prometheus;

namespace WebSocketService.Monitoring;

public static class MetricsRegistry
{
    // Счетчик активных подключений 
    public static readonly Gauge TotalActiveConnections = Metrics
        .CreateGauge("websocket_active_connections_total",
            "Общее количество активных подключений к WebSocket",
            new GaugeConfiguration
            {
                LabelNames = 
                [
                    "service",    // Имя микросервиса
                    "instance"    // ИМя копии микросервиса
                ]
            });
    
    // Страны подключения пользователей
    public static readonly Gauge ActiveConnectionsByGeo = Metrics
        .CreateGauge("websocket_active_connections_by_geo",
            "Активные подключения к WebSocket по странам и городам",
            new GaugeConfiguration
            {
                LabelNames = 
                [
                    "service",
                    "instance",
                    "country", 
                    "city", 
                    "Latitude", 
                    "Longitude"
                ]
            });
}