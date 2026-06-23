using System.Diagnostics;

namespace mTiles.Services;

public sealed class LogTraceListener(FileLogWriter writer) : TraceListener
{
    public override void Write(string? message)
    {
        if (!string.IsNullOrEmpty(message))
            writer.Write("TRACE", message);
    }

    public override void WriteLine(string? message)
    {
        if (!string.IsNullOrEmpty(message))
            writer.Write("TRACE", message);
    }

    public override void TraceEvent(TraceEventCache? eventCache, string source, TraceEventType eventType, int id, string? message)
    {
        if (message is null) return;
        writer.Write(MapLevel(eventType), message);
    }

    public override void TraceEvent(TraceEventCache? eventCache, string source, TraceEventType eventType, int id, string? format, params object?[]? args)
    {
        if (format is null) return;
        var message = args is { Length: > 0 } ? string.Format(format, args) : format;
        writer.Write(MapLevel(eventType), message);
    }

    private static string MapLevel(TraceEventType type) => type switch
    {
        TraceEventType.Critical => "FATAL",
        TraceEventType.Error => "ERROR",
        TraceEventType.Warning => "WARNING",
        TraceEventType.Information => "INFO",
        _ => "TRACE"
    };
}
