using System.Collections.Concurrent;

namespace DevToolbox.Services;

public class InMemoryLoggerService
{
    private ConcurrentQueue<string> _logs = new();
    public event EventHandler<string>? LogsChanged;

    private void OnLogsChanged()
    {
        LogsChanged?.Invoke(this, GetLogs());
    }

    public void Log(string message)
    {
        _logs.Enqueue(message);
        while (_logs.Count > 1000) // Limit to 1000 logs
        {
            _logs.TryDequeue(out _);
        }
        OnLogsChanged();
    }

    public string GetLogs()
    {
        //Create fake logs for testing
        if (_logs.IsEmpty)
        {
            _logs = new ConcurrentQueue<string>(
            [
                "[INFO] Application started.",
                "[DEBUG] Initializing components...",
                "[WARN] Low disk space detected.",
                "[ERROR] Failed to connect to database.",
                "[INFO] Application running.",
                "[DEBUG] Initializing components...",
                "[WARN] Low disk space detected.",
                "[ERROR] Failed to connect to database.",
                "[INFO] Application running.",
                "[DEBUG] Initializing components...",
                "[WARN] Low disk space detected.",
                "[ERROR] Failed to connect to database.",
                "[INFO] Application running."
            ]);
        }

        return string.Join("\n", _logs);
    }

    public void ClearLogs()
    {
        _logs.Clear();
        OnLogsChanged();
    }

}