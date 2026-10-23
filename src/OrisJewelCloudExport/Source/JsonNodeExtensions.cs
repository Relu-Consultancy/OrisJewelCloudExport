using System.Text.Json;
using System.Text.Json.Nodes;

namespace OrisJewelCloudExport.Source;

/// <summary>
/// Tolerant readers for raw JSON: a missing or unexpectedly typed property yields null instead of
/// failing the whole record.
/// </summary>
internal static class JsonNodeExtensions
{
    public static string? GetString(this JsonObject json, string propertyName) =>
        json[propertyName] is JsonValue value ? value.ToRawText() : null;

    public static bool? GetBoolean(this JsonObject json, string propertyName) =>
        json[propertyName] switch
        {
            JsonValue v when v.GetValueKind() == JsonValueKind.True => true,
            JsonValue v when v.GetValueKind() == JsonValueKind.False => false,
            JsonValue v when v.GetValueKind() == JsonValueKind.String && bool.TryParse(v.GetValue<string>(), out bool parsed) => parsed,
            _ => null,
        };

    /// <summary>Strings are returned unescaped and untrimmed; numbers and booleans as their JSON text (7, 1195.00, true).</summary>
    public static string ToRawText(this JsonValue value) =>
        value.GetValueKind() == JsonValueKind.String ? value.GetValue<string>() : value.ToJsonString();
}
