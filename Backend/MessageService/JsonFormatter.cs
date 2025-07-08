using Serilog.Events;
using Serilog.Formatting;

namespace MessageService;

public class JsonFormatter : ITextFormatter
{
    public void Format(LogEvent logEvent, TextWriter output)
    {
        // Записываем только нужные поля в JSON
        var json = new
        {
            @timestamp = logEvent.Timestamp,
            Level = logEvent.Level.ToString(),
            Message = logEvent.RenderMessage()
        };

        output.Write(System.Text.Json.JsonSerializer.Serialize(json));
    }
}