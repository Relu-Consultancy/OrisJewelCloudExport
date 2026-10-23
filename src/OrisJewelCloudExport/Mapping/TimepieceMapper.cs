using OrisJewelCloudExport.Configuration;
using OrisJewelCloudExport.Source;
using C = OrisJewelCloudExport.Mapping.TimepieceColumns;
using N = OrisJewelCloudExport.Mapping.OrisValueNormalizer;

namespace OrisJewelCloudExport.Mapping;

/// <summary>
/// Maps one Oris watch to a JewelCloud Timepieces row. Only columns the source can support are set;
/// every other column stays blank. MAPPING.md documents the reasoning column by column.
/// </summary>
public sealed class TimepieceMapper(ExportSettings settings, MappingDiagnostics diagnostics)
{
    private const string SimpleProduct = "0";
    private const string ActiveProduct = "0";
    private const string InactiveProduct = "1";
    private const string WatchesCategory = "Watches";

    public IReadOnlyDictionary<string, string?> Map(OrisWatch watch)
    {
        string? caseMetal = N.ExtractCaseMetal(watch.Material);
        if (caseMetal is null && N.CleanText(watch.Material) is { } material)
            diagnostics.NoMatch("(case metal)", material);

        // Columns that TimepieceColumnCatalog marks Ambiguous, NoSource or Unused (WatchDialColor, WatchSize,
        // WatchBandMaterial, PriceType, ...) are deliberately never set; the catalog records why for each.
        return new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            [C.IsActive] = MapIsActive(watch.IsArchived),
            [C.ProductType] = SimpleProduct,
            [C.StyleNumber] = N.CleanText(watch.Reference),
            [C.RetailerStockNumber] = N.CleanText(watch.Reference),
            [C.ProductName] = N.CleanText(watch.Name),
            [C.ProductDescription] = N.CleanText(watch.Description),
            [C.MetalType] = Normalize(C.MetalType, caseMetal, m => N.ToMetalType(m, watch.Material)),
            [C.MetalColor] = N.ToMetalColor(caseMetal), // blank is the expected outcome for non-gold cases
            [C.ImagePath] = Normalize(C.ImagePath, watch.ImageUrl, N.ToAbsoluteUrl),
            [C.Collections] = N.CleanText(watch.Collection),
            [C.Categories] = WatchesCategory,
            [C.WatchCaseMaterial] = Normalize(C.WatchCaseMaterial, caseMetal, N.ToWatchCaseMaterial),
            [C.WatchMovement] = Normalize(C.WatchMovement, watch.Winding, N.ToWatchMovement),
            [C.WatchEnergy] = Normalize(C.WatchEnergy, watch.Winding, N.ToWatchEnergy),
            [C.ImagePath2] = Normalize(C.ImagePath2, watch.Image2Url, N.ToAbsoluteUrl),
            [C.RetailPrice] = MapRetailPrice(watch.Price),
        };
    }

    private string? MapIsActive(bool? isArchived)
    {
        if (isArchived is null)
            diagnostics.Note(C.IsActive, "isArchived missing");
        return isArchived switch
        {
            true => InactiveProduct, // archived: still a real product, so inactive rather than "2 - deleted"
            false => ActiveProduct,
            null => null,
        };
    }

    private string? MapRetailPrice(OrisPrice? price)
    {
        if (price is null)
        {
            diagnostics.Note(C.RetailPrice, "no price in source");
            return null;
        }
        if (!string.Equals(N.CleanText(price.Currency), settings.PriceCurrency, StringComparison.OrdinalIgnoreCase))
        {
            diagnostics.Note(C.RetailPrice, $"currency '{price.Currency}' is not {settings.PriceCurrency}");
            return null;
        }
        if (price.Active != true && !settings.IncludeInactivePrices)
        {
            diagnostics.Note(C.RetailPrice, "price.active is not true (IncludeInactivePrices = false)");
            return null;
        }
        return Normalize(C.RetailPrice, price.Amount, N.ToPrice);
    }

    /// <summary>Applies a normalizer and records any present-but-unrecognised source value.</summary>
    private string? Normalize(string column, string? sourceValue, Func<string, string?> normalize)
    {
        string? cleaned = N.CleanText(sourceValue);
        if (cleaned is null) return null;

        string? result = normalize(cleaned);
        if (result is null)
            diagnostics.NoMatch(column, cleaned);
        return result;
    }
}
