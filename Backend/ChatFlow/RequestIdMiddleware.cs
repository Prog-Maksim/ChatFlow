using Serilog.Context;

namespace ChatFlow;

public class RequestIdMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext context)
    {
        const string headerKey = "X-Request-ID";
        string? requestId = context.Request.Headers.ContainsKey(headerKey)
            ? context.Request.Headers[headerKey]
            : Guid.NewGuid().ToString();
        
        context.Items["RequestId"] = requestId;
        LogContext.PushProperty("RequestId", requestId);
        
        context.Response.Headers[headerKey] = requestId;
        await next(context);
    }
}