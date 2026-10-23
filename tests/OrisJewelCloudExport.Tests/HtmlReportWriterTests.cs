using System.Text.Json.Nodes;
using OrisJewelCloudExport.Configuration;
using OrisJewelCloudExport.Mapping;
using OrisJewelCloudExport.Reporting;
using OrisJewelCloudExport.Source;

namespace OrisJewelCloudExport.Tests;

public class HtmlReportWriterTests
{
    private static ExportReport BuildReport(params string[] recordsJson)
    {
        var settings = new ExportSettings();
        var records = recordsJson.Select(j => JsonNode.Parse(j)!.AsObject()).ToList();
        var watches = records.Select(OrisWatch.FromJson).ToList();
        var diagnostics = new MappingDiagnostics();
        var mapper = new TimepieceMapper(settings, diagnostics);
        var rows = watches.Select(mapper.Map).ToList();
        return ExportReportBuilder.Build(settings, watches, rows, SourceTable.Build(records), diagnostics, DateTimeOffset.UnixEpoch);
    }

    [Fact]
    public void Embeds_the_report_data_and_leaves_no_placeholder()
    {
        string html = HtmlReportWriter.Render(BuildReport("""{"reference":"A1","winding":"Automatic winding"}"""));

        Assert.StartsWith("<!doctype html>", html);
        Assert.Contains("\"recordCount\":1", html);
        Assert.DoesNotContain("<script id=\"report-data\" type=\"application/json\">__REPORT_DATA__", html);
    }

    [Fact]
    public void Source_text_cannot_break_out_of_the_data_script()
    {
        string html = HtmlReportWriter.Render(BuildReport("""{"reference":"X","color":"</script><script>alert(1)</script>"}"""));

        Assert.DoesNotContain("<script>alert(1)", html);
        Assert.Equal(2, html.Split("</script>").Length - 1); // only the page's own two script blocks close
    }

    [Fact]
    public void Price_groups_add_up_to_the_record_count()
    {
        var report = BuildReport(
            """{"price":{"currency":"USD","amount":"100.00","active":true}}""",
            """{"price":{"currency":"USD","amount":"200.00","active":false}}""",
            """{"price":null}""",
            """{"price":{"currency":"CHF","amount":"300.00","active":true}}""",
            """{"price":{"currency":"USD","amount":"n/a","active":true}}""");

        var p = report.Price;
        Assert.Equal((1, 1, 1, 1, 1), (p.Exported, p.ExcludedInactive, p.MissingPrice, p.OtherCurrency, p.InvalidAmount));
        Assert.Equal([100.00m], p.ExportedAmounts);
    }
}
