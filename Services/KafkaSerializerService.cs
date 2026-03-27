using System.IO.Compression;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using DevToolbox.Services.Interfaces;

namespace DevToolbox.Services;

public class KafkaSerializerService : IKafkaSerializerService
{
    private static readonly JsonSerializerOptions PrettyOptions = new() { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
    private static readonly JsonSerializerOptions CompactOptions = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

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
        if (node is JsonObject root)
        {
            ProcessMessageProperty(root);
        }
        else if (node is JsonArray rootArr)
        {
            foreach (var element in rootArr)
            {
                if (element is JsonObject obj)
                    ProcessMessageProperty(obj);
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

    public bool ContainsCompressedMessage(string inputJson)
    {
        try
        {
            var node = JsonNode.Parse(inputJson, documentOptions: new JsonDocumentOptions
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip
            });

            if (node is JsonObject root)
                return HasCompressedMessage(root);

            if (node is JsonArray arr)
            {
                foreach (var element in arr)
                    if (element is JsonObject obj && HasCompressedMessage(obj))
                        return true;
            }
        }
        catch { }

        return false;
    }

    private bool HasCompressedMessage(JsonObject obj)
    {
        if (!obj.TryGetPropertyValue("Message", out var messageNode))
            return false;

        if (messageNode is JsonValue mv && mv.TryGetValue<string>(out var messageStr))
        {
            var trimmed = messageStr.AsSpan().TrimStart();
            if (trimmed.Length > 0 && (trimmed[0] == '{' || trimmed[0] == '['))
                return false;

            try
            {
                DecompressGzip(messageStr);
                return true;
            }
            catch { return false; }
        }

        return false;
    }

    private void ProcessMessageProperty(JsonObject obj)
    {
        if (!obj.TryGetPropertyValue("Message", out var messageNode))
            return;

        if (messageNode is JsonValue mv && mv.TryGetValue<string>(out var messageStr))
        {
            var trimmed = messageStr.AsSpan().TrimStart();
            if (trimmed.Length > 0 && (trimmed[0] == '{' || trimmed[0] == '['))
            {
                var parsed = TryParseJson(messageStr);
                if (parsed is not null)
                    obj["Message"] = parsed;
            }
            else
            {
                try
                {
                    var decompressed = DecompressGzip(messageStr);
                    obj["Message"] = JsonNode.Parse(decompressed);
                }
                catch
                {
                    // Not a valid gzip base64 string — leave as-is
                }
            }
        }
    }

    public string EscapeMessageProperty(string prettyJson)
    {
        var node = JsonNode.Parse(prettyJson);
        if (node is null)
            return prettyJson;

        if (node is JsonObject root)
            EscapeMessageInObject(root);
        else if (node is JsonArray arr)
        {
            foreach (var element in arr)
            {
                if (element is JsonObject obj)
                    EscapeMessageInObject(obj);
            }
        }

        return node.ToJsonString(PrettyOptions);
    }

    private static void EscapeMessageInObject(JsonObject obj)
    {
        if (obj.TryGetPropertyValue("Message", out var messageNode) && messageNode is not JsonValue)
        {
            var serialized = messageNode!.ToJsonString(CompactOptions); //Remove CompactOptions to get escaped unicode characters. i.e. \u0022 instead of \"
            obj["Message"] = serialized;
        }
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
