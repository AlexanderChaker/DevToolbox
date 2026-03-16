using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Nodes;
using DevToolbox.Services.Interfaces;

namespace Services;

public class KafkaSerializerService : IKafkaSerializerService
{
    private static readonly JsonSerializerOptions PrettyOptions = new() { WriteIndented = true };

    public string DecompressGzip(string base64GzipString)
    {
        byte[] compressedBytes = Convert.FromBase64String(base64GzipString);

        using var compressedStream = new MemoryStream(compressedBytes);
        using var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress);
        using var reader = new StreamReader(gzipStream);

        return reader.ReadToEnd();
    }

    public string ProcessKafkaMessage(string inputJson)
    {
        var node = JsonNode.Parse(inputJson, documentOptions: new JsonDocumentOptions
        {
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Skip
        });

        if (node is null)
            return inputJson;

        // If the root has a "Message" property that looks like a compressed string, decompress it
        if (node is JsonObject root && root.TryGetPropertyValue("Message", out var messageNode))
        {
            var messageStr = messageNode?.GetValue<string>();
            if (messageStr is not null && !messageStr.TrimStart().StartsWith('{') && !messageStr.TrimStart().StartsWith('['))
            {
                try
                {
                    var decompressed = DecompressGzip(messageStr);
                    root["Message"] = JsonNode.Parse(decompressed);
                }
                catch
                {
                    // Not a valid gzip base64 string — leave as-is
                }
            }
        }

        UnescapeJsonNode(node);

        return node.ToJsonString(PrettyOptions);
    }

    private static void UnescapeJsonNode(JsonNode? node)
    {
        if (node is JsonObject obj)
        {
            foreach (var key in obj.Select(p => p.Key).ToList())
            {
                var value = obj[key];

                if (value is JsonValue jv && jv.TryGetValue<string>(out var str))
                {
                    try
                    {
                        var parsed = JsonNode.Parse(str);
                        if (parsed is not null)
                        {
                            obj[key] = parsed;
                            UnescapeJsonNode(parsed);
                            continue;
                        }
                    }
                    catch
                    {
                        // Not a JSON string — leave as-is
                    }
                }

                UnescapeJsonNode(value);
            }
        }
        else if (node is JsonArray arr)
        {
            for (int i = 0; i < arr.Count; i++)
            {
                var item = arr[i];

                if (item is JsonValue jv && jv.TryGetValue<string>(out var str))
                {
                    try
                    {
                        var parsed = JsonNode.Parse(str);
                        if (parsed is not null)
                        {
                            arr[i] = parsed;
                            UnescapeJsonNode(parsed);
                            continue;
                        }
                    }
                    catch
                    {
                        // Not a JSON string — leave as-is
                    }
                }

                UnescapeJsonNode(item);
            }
        }
    }
}
