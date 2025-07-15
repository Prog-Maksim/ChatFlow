using Serilog.Context;

namespace ChatFlow;

public class RequestIdMiddleware
{
    private readonly RequestDelegate _next;

    public RequestIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        const string headerKey = "X-Request-ID";
        string? requestId = context.Request.Headers.ContainsKey(headerKey)
            ? context.Request.Headers[headerKey]
            : Guid.NewGuid().ToString();

        // Добавляем в HttpContext для логов
        context.Items["RequestId"] = requestId;

        // Добавляем в лог-контекст Serilog
        LogContext.PushProperty("RequestId", requestId);

        // Прокидываем заголовок дальше
        context.Response.Headers[headerKey] = requestId;

        await _next(context);
    }
}