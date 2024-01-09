using System.Globalization;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Json;

namespace Lucca.Infra.Haproxy.AgentCheck;

public class LuccaJsonFormatter : ITextFormatter
{
    private readonly JsonValueFormatter _valueFormatter;

    public LuccaJsonFormatter(JsonValueFormatter? valueFormatter = null)
    {
        _valueFormatter = valueFormatter ?? new JsonValueFormatter(typeTagName: "$type");
    }

    public void Format(LogEvent logEvent, TextWriter output)
    {
        FormatEvent(logEvent, output, _valueFormatter);
        output.WriteLine();
    }

    private static void FormatEvent(LogEvent logEvent, TextWriter output, JsonValueFormatter valueFormatter)
    {
        ArgumentNullException.ThrowIfNull(logEvent);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(valueFormatter);

        output.Write("{\"date\":\"");
        output.Write(logEvent.Timestamp.UtcDateTime.ToString("O"));
        output.Write("\",\"message\":");
        var message = logEvent.MessageTemplate.Render(logEvent.Properties, CultureInfo.InvariantCulture);
        JsonValueFormatter.WriteQuotedJsonString(message, output);

        if (logEvent.Level != LogEventLevel.Information)
        {
            output.Write(",\"level\":\"");
            output.Write(logEvent.Level);
            output.Write('\"');
        }

        if (logEvent.Exception != null)
        {
            output.Write(",\"exception\":");
            JsonValueFormatter.WriteQuotedJsonString(logEvent.Exception.ToString(), output);
        }

        foreach (var (name, value) in logEvent.Properties)
        {
            output.Write(',');
            JsonValueFormatter.WriteQuotedJsonString(name, output);
            output.Write(':');
            valueFormatter.Format(value, output);
        }

        output.Write("}");
    }
}
