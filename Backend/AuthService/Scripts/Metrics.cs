using AuthService.Monitoring;

namespace AuthService.Scripts;

public static class Metrics
{
    /// <summary>
    /// Мониторинг неправильных попыток входа
    /// </summary>
    /// <param name="ip">Ip адрес пользователя</param>
    public static void TrackFailedLogin(string ip)
    {
        MetricsRegistry.BruteForceDetection
            .WithLabels(ip, "auth", Environment.MachineName)
            .Inc();
    }
}