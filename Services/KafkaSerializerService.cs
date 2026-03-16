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

        // If the root has a "Message" property, try to decompress or parse it
        if (node is JsonObject root && root.TryGetPropertyValue("Message", out var messageNode))
        {
            if (messageNode is JsonValue mv && mv.TryGetValue<string>(out var messageStr))
            {
                var trimmed = messageStr.AsSpan().TrimStart();
                if (trimmed.Length > 0 && (trimmed[0] == '{' || trimmed[0] == '['))
                {
                    // Escaped JSON string — parse directly
                    var parsed = TryParseJson(messageStr);
                    if (parsed is not null)
                        root["Message"] = parsed;
                }
                else
                {
                    // Possibly gzip-compressed base64
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
        }

        UnescapeJsonNode(node);

        return node.ToJsonString(PrettyOptions);
    }

    private static bool LooksLikeJson(string s)
    {
        var span = s.AsSpan().TrimStart();
        return span.Length > 1 && (span[0] == '{' || span[0] == '[');
    }

    private static JsonNode? TryParseJson(string str)
    {
        if (!LooksLikeJson(str))
            return null;

        try
        {
            return JsonNode.Parse(str);
        }
        catch
        {
            return null;
        }
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
                    var parsed = TryParseJson(str);
                    if (parsed is not null)
                    {
                        obj[key] = parsed;
                        UnescapeJsonNode(parsed);
                        continue;
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
                    var parsed = TryParseJson(str);
                    if (parsed is not null)
                    {
                        arr[i] = parsed;
                        UnescapeJsonNode(parsed);
                        continue;
                    }
                }

                UnescapeJsonNode(item);
            }
        }
    }
}
