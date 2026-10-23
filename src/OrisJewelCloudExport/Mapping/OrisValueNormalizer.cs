using System.Globalization;
using System.Text.RegularExpressions;

namespace OrisJewelCloudExport.Mapping;

/// <summary>
/// Converts Oris source values into JewelCloud accepted values (VALUES sheet of the template).
/// Every method returns null when the source value is missing or not recognised: a blank cell is
/// preferred over a guessed value.
/// </summary>
public static partial class OrisValueNormalizer
{
    private enum WindingKind { Unknown, Automatic, Hand }

    // Keys are the lower-cased case metal returned by ExtractCaseMetal.
    // Oris metals with no matching JewelCloud value (bronze, stainless steel/bronze, carbon fibre) are absent on purpose.
    private static readonly Dictionary<string, string> WatchCaseMaterialByMetal = new(StringComparer.Ordinal)
    {
        ["stainless steel"] = "Stainless Steel",
        ["titanium"] = "Titanium",
        ["18k rose gold"] = "Rose Gold",
        ["18k yellow gold"] = "Yellow Gold",
    };

    private static readonly Dictionary<string, string> MetalTypeByMetal = new(StringComparer.Ordinal)
    {
        ["stainless steel"] = "Steel: Stainless",
        ["titanium"] = "Titanium",
        ["bronze"] = "Bronze",
        ["carbon fibre"] = "Carbon",
        ["18k rose gold"] = "18K Rose Gold",
        ["18k yellow gold"] = "18K Gold", // the colour goes to MetalColor
    };

    private static readonly Dictionary<string, string> MetalColorByMetal = new(StringComparer.Ordinal)
    {
        ["18k rose gold"] = "Rose",
        ["18k yellow gold"] = "Yellow",
    };

    /// <summary>Trims surrounding whitespace, including the stray trailing CR/LF some Oris values carry. Blank → null.</summary>
    public static string? CleanText(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>"Automatic winding…" → Automatic; "Hand winding" → Manual Mechanical.</summary>
    public static string? ToWatchEnergy(string? winding) => ClassifyWinding(winding) switch
    {
        WindingKind.Automatic => "Automatic",
        WindingKind.Hand => "Manual Mechanical",
        _ => null,
    };

    /// <summary>Self-winding (automatic) and hand-wound movements are both mechanical movements.</summary>
    public static string? ToWatchMovement(string? winding) =>
        ClassifyWinding(winding) is WindingKind.Automatic or WindingKind.Hand ? "Mechanical" : null;

    private static WindingKind ClassifyWinding(string? winding)
    {
        string? text = CleanText(winding);
        if (text is null) return WindingKind.Unknown;
        // Variants seen: "Automatic winding", "Automatic winding, with red rotor", "Hand winding"
        if (text.StartsWith("Automatic winding", StringComparison.OrdinalIgnoreCase)) return WindingKind.Automatic;
        if (text.StartsWith("Hand winding", StringComparison.OrdinalIgnoreCase)) return WindingKind.Hand;
        return WindingKind.Unknown;
    }

    /// <summary>
    /// Extracts the case metal from the material text, lower-cased:
    /// "Multi-piece stainless steel case, rose gold PVD plated top ring" → "stainless steel".
    /// Only the first phrase describes the case; later phrases describe the top ring, plating or lugs.
    /// </summary>
    public static string? ExtractCaseMetal(string? material)
    {
        string? text = CleanText(material);
        if (text is null) return null;
        var match = CaseMetalPattern().Match(text);
        return match.Success ? match.Groups["metal"].Value.Trim().ToLowerInvariant() : null;
    }

    // "Multi-piece <metal> case[, ; or ' and' …]" or a bare "Multi-piece <metal>" (e.g. "Multi-piece carbon fibre").
    [GeneratedRegex(@"^(?:multi-piece\s+)?(?<metal>[^,;]+?)(?:\s+case\b|\s*[,;]|\s*$)", RegexOptions.IgnoreCase)]
    private static partial Regex CaseMetalPattern();

    public static string? ToWatchCaseMaterial(string? caseMetal) =>
        caseMetal is null ? null : WatchCaseMaterialByMetal.GetValueOrDefault(caseMetal);

    /// <summary>
    /// The product's metal. A steel case with a solid 18K gold top ring is a bi-metal piece
    /// ("Steel: Stainless with Gold"); gold PVD or gold plating does not count as gold.
    /// </summary>
    public static string? ToMetalType(string? caseMetal, string? material)
    {
        if (caseMetal is null) return null;
        if (caseMetal == "stainless steel" && material is not null && Solid18KGoldPattern().IsMatch(material))
            return "Steel: Stainless with Gold";
        return MetalTypeByMetal.GetValueOrDefault(caseMetal);
    }

    [GeneratedRegex(@"\b18K\s+(?:rose\s+|yellow\s+|white\s+)?gold\b", RegexOptions.IgnoreCase)]
    private static partial Regex Solid18KGoldPattern();

    /// <summary>Only gold cases state their colour explicitly; for every other metal the colour is unknown.</summary>
    public static string? ToMetalColor(string? caseMetal) =>
        caseMetal is null ? null : MetalColorByMetal.GetValueOrDefault(caseMetal);

    /// <summary>
    /// Absolute http(s) URL with unsafe characters percent-encoded (Oris image URLs contain spaces),
    /// or null if the value is not a usable URL.
    /// </summary>
    public static string? ToAbsoluteUrl(string? url)
    {
        string? text = CleanText(url);
        return text is not null
               && Uri.TryCreate(text, UriKind.Absolute, out var uri)
               && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            ? uri.AbsoluteUri
            : null;
    }

    /// <summary>Positive decimal amount in invariant format ("1195.00" → "1195.00"), or null.</summary>
    public static string? ToPrice(string? amount) =>
        decimal.TryParse(CleanText(amount), NumberStyles.Number, CultureInfo.InvariantCulture, out decimal value) && value > 0
            ? value.ToString(CultureInfo.InvariantCulture)
            : null;
}
