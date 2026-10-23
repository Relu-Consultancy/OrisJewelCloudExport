using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace OrisJewelCloudExport.Source;

/// <summary>
/// The SRC_ columns: every property of every source record, flattened into one column per value.
/// Built from the raw JSON rather than <see cref="OrisWatch"/>, so properties the API adds later
/// (e.g. "waterResistance") are exported without any code change.
/// </summary>
/// <remarks>
/// Conventions:
/// <list type="bullet">
/// <item>Column name = "SRC_" + JSON property name, unchanged.</item>
/// <item>Nested objects are flattened with "_": price.amount → SRC_price_amount.</item>
/// <item>Arrays are kept as JSON text in a single column.</item>
/// <item>Strings are written verbatim (not trimmed); numbers and booleans as their JSON text.</item>
/// <item>JSON null and a missing property are both written as an empty cell.</item>
/// <item>Columns appear in the order they are first seen across all records.</item>
/// </list>
/// </remarks>
public sealed class SourceTable
{
    public const string Prefix = "SRC_";
    private const string Separator = "_";

    private static readonly JsonSerializerOptions ReadableJson = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    private SourceTable(IReadOnlyList<string> columns, IReadOnlyList<IReadOnlyDictionary<string, string>> rows)
    {
        Columns = columns;
        Rows = rows;
    }

    public IReadOnlyList<string> Columns { get; }

    /// <summary>One entry per source record, in input order. A record has no entry for columns it lacks.</summary>
    public IReadOnlyList<IReadOnlyDictionary<string, string>> Rows { get; }

    public static SourceTable Build(IReadOnlyList<JsonObject> records)
    {
        var columns = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var collisions = new HashSet<string>(StringComparer.Ordinal);
        var rows = new List<IReadOnlyDictionary<string, string>>(records.Count);

        foreach (var record in records)
        {
            var row = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var (column, value) in Flatten(record))
            {
                // e.g. a top-level "price_amount" property next to price.amount
                if (!row.TryAdd(column, value))
                {
                    if (collisions.Add(column))
                        Log.Warn($"Source column name collision on '{column}'; only the first value per record is kept.");
                    continue;
                }
                if (seen.Add(column))
                    columns.Add(column);
            }
            rows.Add(row);
        }

        // "price": null produces a bare SRC_price cell. When other records carry the object's children
        // (SRC_price_amount, ...), that bare column is empty everywhere and redundant, so drop it.
        var redundant = columns
            .Where(c => columns.Any(other => other.StartsWith(c + Separator, StringComparison.Ordinal))
                        && rows.All(r => string.IsNullOrEmpty(r.GetValueOrDefault(c))))
            .ToHashSet(StringComparer.Ordinal);

        return new SourceTable(columns.Where(c => !redundant.Contains(c)).ToList(), rows);
    }

    internal static List<KeyValuePair<string, string>> Flatten(JsonObject record)
    {
        var cells = new List<KeyValuePair<string, string>>();
        foreach (var (name, value) in record)
            AddCells(cells, Prefix + name, value);
        return cells;
    }

    private static void AddCells(List<KeyValuePair<string, string>> cells, string column, JsonNode? node)
    {
        switch (node)
        {
            case JsonObject obj when obj.Count > 0:
                foreach (var (name, child) in obj)
                    AddCells(cells, column + Separator + name, child);
                break;
            case JsonValue value:
                cells.Add(new(column, value.ToRawText()));
                break;
            case null:
                cells.Add(new(column, string.Empty));
                break;
            default: // arrays and empty objects: keep the JSON text so nothing is lost
                cells.Add(new(column, node.ToJsonString(ReadableJson)));
                break;
        }
    }
}
