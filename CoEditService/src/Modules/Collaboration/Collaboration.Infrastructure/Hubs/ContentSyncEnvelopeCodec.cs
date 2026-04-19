using System.Text.Json;
using System.Text.Json.Serialization;

namespace Collaboration.Infrastructure.Hubs;

internal static class ContentSyncEnvelopeCodec
{
        private sealed record SnapshotEnvelope(
        [property: JsonPropertyName("v")] int V,
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("content")] string Content);

    private sealed record OperationEnvelope(
        [property: JsonPropertyName("v")] int V,
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("baseLength")] int BaseLength,
        [property: JsonPropertyName("start")] int Start,
        [property: JsonPropertyName("deleteCount")] int DeleteCount,
        [property: JsonPropertyName("insert")] string Insert);

    internal sealed record DecodeResult(string Content, string OutboundPayload, bool ShouldResyncCaller);

    public static DecodeResult DecodeIncoming(string incomingPayload, string currentContent)
    {
        var normalizedCurrent = currentContent ?? string.Empty;
        var normalizedIncoming = incomingPayload ?? string.Empty;

        if (string.IsNullOrEmpty(normalizedIncoming))
        {
            var snapshot = SerializeSnapshot(string.Empty);
            return new DecodeResult(string.Empty, snapshot, false);
        }

        if (!TryParseEnvelope(normalizedIncoming, out var envelope))
        {
            var snapshot = SerializeSnapshot(normalizedIncoming);
            return new DecodeResult(normalizedIncoming, snapshot, false);
        }

        if (envelope is SnapshotEnvelope snapshotEnvelope)
        {
            var content = snapshotEnvelope.Content ?? string.Empty;
            return new DecodeResult(content, SerializeSnapshot(content), false);
        }

        var operation = (OperationEnvelope)envelope;
        var updatedContent = TryApplyOperation(normalizedCurrent, operation);
        if (updatedContent is null)
        {
            var snapshot = SerializeSnapshot(normalizedCurrent);
            return new DecodeResult(normalizedCurrent, snapshot, true);
        }

        var outboundOperation = SerializeOperation(operation.BaseLength, operation.Start, operation.DeleteCount, operation.Insert);
        return new DecodeResult(updatedContent, outboundOperation, false);
    }

    public static string SerializeSnapshot(string content)
    {
        return JsonSerializer.Serialize(new SnapshotEnvelope(1, "snapshot", content ?? string.Empty));
    }

    private static string SerializeOperation(int baseLength, int start, int deleteCount, string insert)
    {
        return JsonSerializer.Serialize(new OperationEnvelope(1, "op", baseLength, start, deleteCount, insert ?? string.Empty));
    }

    private static bool TryParseEnvelope(string payload, out object envelope)
    {
        envelope = null!;

        try
        {
            using var document = JsonDocument.Parse(payload);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            if (!TryGetString(document.RootElement, "type", out var type))
            {
                return false;
            }

            if (string.Equals(type, "snapshot", StringComparison.OrdinalIgnoreCase))
            {
                var content = TryGetString(document.RootElement, "content", out var value)
                    ? value
                    : string.Empty;

                envelope = new SnapshotEnvelope(1, "snapshot", content);
                return true;
            }

            if (!string.Equals(type, "op", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!TryGetInt(document.RootElement, "baseLength", out var baseLength) ||
                !TryGetInt(document.RootElement, "start", out var start) ||
                !TryGetInt(document.RootElement, "deleteCount", out var deleteCount))
            {
                return false;
            }

            var insert = TryGetString(document.RootElement, "insert", out var parsedInsert)
                ? parsedInsert
                : string.Empty;

            envelope = new OperationEnvelope(1, "op", baseLength, start, deleteCount, insert);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryGetInt(JsonElement element, string propertyName, out int value)
    {
        value = default;
        return TryGetProperty(element, propertyName, out var property) &&
               property.ValueKind == JsonValueKind.Number &&
               property.TryGetInt32(out value);
    }

    private static bool TryGetString(JsonElement element, string propertyName, out string value)
    {
        value = string.Empty;
        if (!TryGetProperty(element, propertyName, out var property) ||
            property.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        value = property.GetString() ?? string.Empty;
        return true;
    }

    private static bool TryGetProperty(JsonElement element, string propertyName, out JsonElement property)
    {
        if (element.TryGetProperty(propertyName, out property))
        {
            return true;
        }

        foreach (var candidate in element.EnumerateObject())
        {
            if (string.Equals(candidate.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                property = candidate.Value;
                return true;
            }
        }

        property = default;
        return false;
    }

    private static string? TryApplyOperation(string current, OperationEnvelope operation)
    {
        if (operation.BaseLength != current.Length)
        {
            return null;
        }

        if (operation.Start < 0 || operation.DeleteCount < 0)
        {
            return null;
        }

        if (operation.Start > current.Length)
        {
            return null;
        }

        if (operation.Start + operation.DeleteCount > current.Length)
        {
            return null;
        }

        return current[..operation.Start] +
               operation.Insert +
               current[(operation.Start + operation.DeleteCount)..];
    }
    
}