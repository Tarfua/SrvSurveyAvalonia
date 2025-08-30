using System.Collections.Concurrent;
using System.Text;

namespace SrvSurvey.Core;

public static class Logging
{
    private static readonly ConcurrentQueue<string> _buffer = new();
    private static readonly object _sync = new();
    public static event Action<string>? Message;

    public static void Info(string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss}] {message}";
        _buffer.Enqueue(line);
        Message?.Invoke(line);
    }

    public static string Drain()
    {
        var sb = new StringBuilder();
        while (_buffer.TryDequeue(out var line))
            sb.AppendLine(line);
        return sb.ToString();
    }
}


