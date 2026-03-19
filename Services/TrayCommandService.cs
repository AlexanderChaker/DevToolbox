namespace DevToolbox.Services;

public class TrayCommandService
{
    public static TrayCommandService? Instance { get; private set; }

    public TrayCommandService()
    {
        Instance = this;
    }

    public event Action? KafkaDeserializeRequested;
    public bool PendingKafkaDeserialize { get; set; }

    public void RequestKafkaDeserialize()
    {
        PendingKafkaDeserialize = true;
        KafkaDeserializeRequested?.Invoke();
    }
}
