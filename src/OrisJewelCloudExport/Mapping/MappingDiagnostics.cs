namespace OrisJewelCloudExport.Mapping;

/// <summary>
/// Counts why mapped columns were left blank, so unrecognised or new source values are visible in
/// the run summary instead of disappearing silently.
/// </summary>
public sealed class MappingDiagnostics
{
    private readonly Dictionary<(string Column, string Message), int> _counts = [];

    /// <summary>A source value was present but has no accepted JewelCloud value.</summary>
    public void NoMatch(string column, string sourceValue) => Record(column, $"no accepted value for '{sourceValue}'");

    /// <summary>A column was left blank for a known reason (e.g. no price in the source).</summary>
    public void Note(string column, string reason) => Record(column, reason);

    public IEnumerable<(string Column, string Message, int Count)> Entries =>
        _counts.Select(e => (e.Key.Column, e.Key.Message, e.Value))
               .OrderBy(e => e.Column, StringComparer.Ordinal)
               .ThenByDescending(e => e.Value);

    private void Record(string column, string message)
    {
        var key = (column, message);
        _counts[key] = _counts.GetValueOrDefault(key) + 1;
    }
}
