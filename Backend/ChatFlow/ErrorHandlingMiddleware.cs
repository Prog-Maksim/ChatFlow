namespace ChatFlow;

public class ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
{
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
        
        if (context.Response.StatusCode >= 500)
        {
            // проверим, что тело ещё не отправлено
            if (!context.Response.HasStarted)
            {
                var requestId = context.Items["RequestId"]?.ToString();
                context.Response.ContentType = "application/json";

                var result = new
                {
                    error = "Internal server error",
                    requestId = requestId
                };

                var json = System.Text.Json.JsonSerializer.Serialize(result);
                await context.Response.WriteAsync(json);
            }
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        var requestId = context.Items["RequestId"]?.ToString();

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";

        var result = new
        {
            error = "Unexpected server error",
            requestId = requestId
        };

        var json = System.Text.Json.JsonSerializer.Serialize(result);
        return context.Response.WriteAsync(json);
    }
}