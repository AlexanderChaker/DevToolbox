using System.Collections.Concurrent;

namespace DevToolbox.Services;

public class InMemoryLoggerService
{
    private readonly ConcurrentQueue<string> _logs = new();

    public void Log(string message)
    {
        _logs.Enqueue(message);
        while (_logs.Count > 1000) // Limit to 1000 logs
        {
            _logs.TryDequeue(out _);
        }
    }

    public string GetLogs() => string.Join("\n", _logs);

    public void ClearLogs() => _logs.Clear();

}