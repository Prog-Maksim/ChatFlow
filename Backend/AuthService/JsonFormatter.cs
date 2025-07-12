using System.Text.Json;
using System.Text.Json.Serialization;
using Serilog.Events;
using Serilog.Formatting;

namespace AuthService;

public class JsonFormatter : ITextFormatter
{
    public void Format(LogEvent logEvent, TextWriter output)
    {
        var exception = logEvent.Exception;

        var logObject = new
        {
            @timestamp = logEvent.Timestamp.ToString("o"),
            Level = logEvent.Level.ToString(),
            Message = logEvent.RenderMessage(),
            Service = "AuthService",

            // Детали исключения (если есть)
            Exception = exception?.Message,
            ExceptionType = exception?.GetType().FullName,
            StackTrace = exception?.StackTrace,

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