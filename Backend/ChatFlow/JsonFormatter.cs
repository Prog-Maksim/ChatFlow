using System.Text.Json;
using System.Text.Json.Serialization;
using Serilog.Events;
using Serilog.Formatting;

namespace ChatFlow;

public class JsonFormatter : ITextFormatter
{
    public void Format(LogEvent logEvent, TextWriter output)
    {
        logEvent.Properties.TryGetValue("RequestId", out var requestIdValue);
        var exception = logEvent.Exception;

        var logObject = new
        {
            @timestamp = logEvent.Timestamp.ToString("o"),
            Level = logEvent.Level.ToString(),
            Message = logEvent.RenderMessage(),
            Service = "ChatService",
            
            RequestId = requestIdValue?.ToString().Trim('"'),

            // Детали исключения (если есть)
            Exception = exception?.Message,
            ExceptionType = exception?.GetType().FullName,
            exception?.StackTrace,

            // Подробности из StackTrace
            ErrorSource = exception?.TargetSite?.DeclaringType?.FullName,
            Method = exception?.TargetSite?.Name,
            File = GetFileNameFromStackTrace(exception),
            Line = GetLineNumberFromStackTrace(exception)
        };

        var jsonOptions = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };

        output.Write(JsonSerializer.Serialize(logObject, jsonOptions));
    }

    // Извлекаем имя файла из стека (если есть)
    private static string? GetFileNameFromStackTrace(Exception? exception)
    {
        if (exception == null) return null;

        var stackTrace = new System.Diagnostics.StackTrace(exception, true);
        var frame = stackTrace.GetFrames()?.FirstOrDefault(f => f.GetFileName() != null);
        return frame?.GetFileName();
    }

    // Извлекаем номер строки из стека (если есть)
    private static int? GetLineNumberFromStackTrace(Exception? exception)
    {
        if (exception == null) return null;

        var stackTrace = new System.Diagnostics.StackTrace(exception, true);
        var frame = stackTrace.GetFrames()?.FirstOrDefault(f => f.GetFileLineNumber() > 0);
        return frame?.GetFileLineNumber();
    }
}