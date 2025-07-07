using Prometheus;

namespace ProfileService.Monitoring;

public static class MetricsRegistry
{
    // Счетчик
    public static readonly Counter EndpointRequestCounter = Metrics
        .CreateCounter("profileservice_requests_total",
            "Total number of endpoint requests",
            new CounterConfiguration
            {
                LabelNames =
                [
                    "endpoint",   // Имя ручки
                    "method",     // Метод запроса
                    "service",    // Имя микросервиса
                    "instance"    // Имя копии микросервиса
                ]
            });
    
    // Гистограмма (время ответа)
    public static readonly Histogram EndpointDuration = Metrics.CreateHistogram(
        "profileservice_request_duration_seconds",
        "Request duration by endpoint",
        new HistogramConfiguration
        {
            Buckets = Histogram.ExponentialBuckets(0.01, 2, 10), // от 10мс до ~10сек
            LabelNames = [ "endpoint", "method", "service", "instance" ]
        });
}