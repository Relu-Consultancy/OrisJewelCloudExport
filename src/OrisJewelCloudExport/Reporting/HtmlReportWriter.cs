using System.Reflection;
using System.Text;
using System.Text.Json;

namespace OrisJewelCloudExport.Reporting;

/// <summary>
/// Writes the run report as one self-contained HTML file: the page template (an embedded resource) with
/// the report data injected as JSON. No server and no external scripts are needed to view it.
/// </summary>
public static class HtmlReportWriter
{
    private const string TemplateResource = "OrisJewelCloudExport.ReportTemplate.html";
    private const string DataPlaceholder = "__REPORT_DATA__";

    private const string DocumentStart =
        "<!doctype html>\n<html lang=\"en\">\n<meta charset=\"utf-8\">\n" +
        "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1, viewport-fit=cover\">\n";

    // The default encoder escapes <, >, & and quotes, so source text can never close the <script> tag it sits in.
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static void Write(string path, ExportReport report)
    {
        string fullPath = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, Render(report), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    internal static string Render(ExportReport report)
    {
        string template = LoadTemplate();
        if (!template.Contains(DataPlaceholder, StringComparison.Ordinal))
            throw new InvalidOperationException($"Report template is missing the {DataPlaceholder} placeholder.");

        return DocumentStart + template.Replace(DataPlaceholder, JsonSerializer.Serialize(report, JsonOptions));
    }

    private static string LoadTemplate()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(TemplateResource)
            ?? throw new InvalidOperationException($"Embedded resource '{TemplateResource}' not found.");
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }
}
