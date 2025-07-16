using ChatFlow.Monitoring;

namespace ChatFlow.Scripts;

public static class Metrics
{
    /// <summary>
    /// Мониторинг неправильных попыток входа
    /// </summary>
    /// <param name="ip">Ip адрес пользователя</param>
    public static void TrackFailedLogin(string ip)
    {
        MetricsRegistry.BruteForceDetection
            .WithLabels(ip)
            .Inc();
    }
}