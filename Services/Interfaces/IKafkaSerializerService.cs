namespace DevToolbox.Services.Interfaces;

public interface IKafkaSerializerService
{
    string DecompressGzip(string base64GzipString);
    string ProcessKafkaMessage(string inputJson);
    string EscapeMessageProperty(string prettyJson);
}
