using Prometheus;

namespace AuthService.Monitoring;

public static class AuthMetrics
{
    /// <summary>
    /// Количество успешных регистраций пользователей
    /// </summary>
    public static Counter RegistrationSuccessCounter = Metrics
        .CreateCounter("user_registration_success_total", "Количество успешных регистраций пользователей");
    
    public static void SetRegistrationSuccessCounter(Counter counter)
    {
        RegistrationSuccessCounter = counter;
    }

    /// <summary>
    /// Количество неудачных регистраций из-за занятого номера
    /// </summary>
    public static Counter RegistrationFailureCounter = Metrics
        .CreateCounter("user_registration_failure_total", "Количество неудачных регистраций из-за занятого номера");

    
    /// <summary>
    /// Время выполнения регистрации пользователя
    /// </summary>
    public static Histogram RegistrationDurationHistogram = Metrics
        .CreateHistogram("user_registration_duration_seconds", "Время выполнения регистрации пользователя");
}