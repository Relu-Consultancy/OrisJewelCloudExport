using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;

namespace OrisJewelCloudExport.Output;

/// <summary>
/// Writes RFC 4180 CSV as UTF-8 with BOM: comma-delimited, CRLF row endings, and any field containing
/// a comma, double quote, CR or LF (or with leading/trailing spaces) wrapped in quotes with inner quotes doubled.
/// </summary>
public static class CsvFileWriter
{
    private static readonly char[] CharsRequiringQuotes = [',', '"', '\r', '\n'];

    public static void Write(string path, IReadOnlyList<string> header, IEnumerable<IReadOnlyList<string?>> rows)
    {
        string fullPath = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        // Write to a temp file and swap it in, so a failed run never leaves a half-written CSV behind.
        string tempPath = fullPath + ".tmp";
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            // Explicit rule rather than CsvHelper's default, which only checks for the full NewLine
            // sequence once NewLine is configured and would leave a lone LF unquoted.
            ShouldQuote = args => NeedsQuotes(args.Field),
        };

        try
        {
            using (var writer = new StreamWriter(tempPath, append: false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true)))
            using (var csv = new CsvWriter(writer, config))
            {
                WriteRecord(csv, header);
                foreach (var row in rows)
                {
                    if (row.Count != header.Count)
                        throw new InvalidOperationException($"Row has {row.Count} values but the header has {header.Count} columns.");
                    WriteRecord(csv, row);
                }
            }
            File.Move(tempPath, fullPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    internal static bool NeedsQuotes(string? field) =>
        !string.IsNullOrEmpty(field)
        && (field.IndexOfAny(CharsRequiringQuotes) >= 0 || char.IsWhiteSpace(field[0]) || char.IsWhiteSpace(field[^1]));

    private static void WriteRecord(CsvWriter csv, IEnumerable<string?> fields)
    {
        foreach (string? field in fields)
            csv.WriteField(field ?? string.Empty);
        csv.NextRecord();
    }
}
