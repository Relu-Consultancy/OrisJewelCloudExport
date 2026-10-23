using System.Globalization;
using System.Text;
using CsvHelper;
using OrisJewelCloudExport.Output;

namespace OrisJewelCloudExport.Tests;

public sealed class CsvFileWriterTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"csvwriter-{Guid.NewGuid():N}.csv");

    public void Dispose() => File.Delete(_path);

    [Fact]
    public void Writes_utf8_with_bom()
    {
        CsvFileWriter.Write(_path, ["A"], [["Hölstein"]]);

        byte[] bytes = File.ReadAllBytes(_path);
        Assert.Equal([0xEF, 0xBB, 0xBF], bytes[..3]);
        Assert.Contains("Hölstein", Encoding.UTF8.GetString(bytes));
    }

    [Fact]
    public void Special_characters_round_trip_without_breaking_rows()
    {
        string?[] values =
        [
            "comma, inside",
            "BC4 \"Der Meisterflieger\"",
            "line\r\nbreak",
            "lone\nLF",
            "lone\rCR",
            " leading space",
            null,
            "Regulateur “Der Meistertaucher”",
        ];
        string[] header = values.Select((_, i) => $"C{i}").ToArray();

        CsvFileWriter.Write(_path, header, [values, values]);

        using var reader = new CsvReader(new StreamReader(_path), CultureInfo.InvariantCulture);
        var records = new List<string[]>();
        while (reader.Read())
            records.Add(Enumerable.Range(0, header.Length).Select(i => reader.GetField(i)!).ToArray());

        Assert.Equal(3, records.Count); // header + 2 rows: embedded line breaks did not split rows
        Assert.Equal(header, records[0]);
        Assert.Equal(values.Select(v => v ?? ""), records[1]);
    }

    [Fact]
    public void Rejects_rows_that_do_not_match_the_header()
    {
        Assert.Throws<InvalidOperationException>(() => CsvFileWriter.Write(_path, ["A", "B"], [["only one"]]));
    }

    [Theory]
    [InlineData("plain", false)]
    [InlineData("a,b", true)]
    [InlineData("a\"b", true)]
    [InlineData("a\nb", true)]
    [InlineData("a\rb", true)]
    [InlineData("trailing ", true)]
    [InlineData("", false)]
    public void Quotes_only_when_needed(string field, bool expected) =>
        Assert.Equal(expected, CsvFileWriter.NeedsQuotes(field));
}
