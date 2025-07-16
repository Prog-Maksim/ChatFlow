using Prometheus;

namespace ChatFlow.Monitoring;

public static class MetricsRegistry
{
    // Счетчик
    public static readonly Counter EndpointRequestCounter = Metrics
        .CreateCounter("authservice_requests_total",
            "Total number of endpoint requests",
            new CounterConfiguration
            {
                LabelNames =
                [
                    "endpoint",   // Имя ручки
                    "method"     // Метод запроса
                ]
            });
    
    // Гистограмма (время ответа)
    public static readonly Histogram EndpointDuration = Metrics.CreateHistogram(
        "authservice_request_duration_seconds",
        "Request duration by endpoint",
        new HistogramConfiguration
        {
            Buckets = Histogram.ExponentialBuckets(0.01, 2, 10), // от 10мс до ~10сек
            LabelNames = [ "endpoint", "method" ]
        });
    
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