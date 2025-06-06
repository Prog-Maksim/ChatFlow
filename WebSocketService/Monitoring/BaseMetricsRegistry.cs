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
}