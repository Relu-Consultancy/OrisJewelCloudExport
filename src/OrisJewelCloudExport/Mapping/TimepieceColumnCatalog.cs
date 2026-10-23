using C = OrisJewelCloudExport.Mapping.TimepieceColumns;

namespace OrisJewelCloudExport.Mapping;

public enum ColumnTreatment
{
    /// <summary>Populated from a source field, directly or through a normalization rule.</summary>
    Mapped,

    /// <summary>The same value for every row, justified by the brief.</summary>
    Fixed,

    /// <summary>The source has related data, but its meaning is too unclear to map safely.</summary>
    Ambiguous,

    /// <summary>The source has no field for this column.</summary>
    NoSource,

    /// <summary>Free-form column with no agreed use.</summary>
    Unused,
}

public sealed record ColumnInfo(string Name, ColumnTreatment Treatment, string? Source, string Rule);

/// <summary>
/// How each Timepieces column is filled, and why the blank ones stay blank. Shown in the HTML report and
/// checked by tests against what <see cref="TimepieceMapper"/> actually produces. MAPPING.md has the long form.
/// </summary>
public static class TimepieceColumnCatalog
{
    public static readonly IReadOnlyList<ColumnInfo> All =
    [
        new(C.IsActive, ColumnTreatment.Mapped, "isArchived", "Archived → 1 (inactive), otherwise 0 (active)"),
        new(C.ProductType, ColumnTreatment.Fixed, null, "0 (Simple Product), per the brief"),
        new(C.StyleNumber, ColumnTreatment.Mapped, "reference", "Oris reference, trimmed"),
        new(C.RetailerStockNumber, ColumnTreatment.Mapped, "reference", "Same as StyleNumber"),
        new(C.ProductName, ColumnTreatment.Mapped, "name", "Trimmed"),
        new(C.ProductDescription, ColumnTreatment.Mapped, "description", "Trimmed"),
        new(C.MetalType, ColumnTreatment.Mapped, "material", "Case metal; steel with a solid 18K gold top ring → Steel: Stainless with Gold"),
        new(C.MetalColor, ColumnTreatment.Mapped, "material", "Gold cases only, which name their colour (Rose, Yellow)"),
        new(C.ImagePath, ColumnTreatment.Mapped, "imageUrl", "Absolute URL with spaces encoded"),
        new(C.Collections, ColumnTreatment.Mapped, "collection", "Trimmed"),
        new(C.Categories, ColumnTreatment.Fixed, null, "Watches: the endpoint only returns watches"),
        new(C.WatchBandMaterial, ColumnTreatment.NoSource, null, "No strap or bracelet field"),
        new(C.WatchBandType, ColumnTreatment.NoSource, null, "No strap or bracelet field"),
        new(C.WatchCaseMaterial, ColumnTreatment.Mapped, "material", "Case metal; bronze, steel/bronze and carbon fibre have no accepted value"),
        new(C.WatchCaseShape, ColumnTreatment.NoSource, null, "No case shape field"),
        new(C.WatchCrystalType, ColumnTreatment.NoSource, null, "No crystal field"),
        new(C.WatchDialColor, ColumnTreatment.Ambiguous, "color", "color may be the dial, strap or case colour"),
        new(C.WatchDisplayType, ColumnTreatment.NoSource, null, "No display or complication field"),
        new(C.WatchMovement, ColumnTreatment.Mapped, "winding", "Automatic and hand winding → Mechanical"),
        new(C.WatchNumberType, ColumnTreatment.Ambiguous, "winding", "Accepted values mix dial numerals with movement types"),
        new(C.WatchEnergy, ColumnTreatment.Mapped, "winding", "Automatic winding → Automatic; hand winding → Manual Mechanical"),
        new(C.Gender, ColumnTreatment.NoSource, null, "No gender field"),
        new(C.PriceType, ColumnTreatment.Ambiguous, null, "Pricing policy is a retailer decision, not product data"),
        new(C.DeliveryTime, ColumnTreatment.NoSource, null, "No delivery field; JewelCloud defaults to 2 Weeks"),
        new(C.WatchSize, ColumnTreatment.Ambiguous, "size", "Size is in mm; JewelCloud's Mini to Extra Extra Large ranges are not defined"),
        new(C.WatchType, ColumnTreatment.Ambiguous, "collection", "Could only be guessed from collection names"),
        new(C.ImagePath2, ColumnTreatment.Mapped, "image2Url", "Absolute URL with spaces encoded"),
        new(C.RetailPrice, ColumnTreatment.Mapped, "price.amount", "Positive amount in the configured currency; active prices only unless configured"),
        new(C.ExtendedField1, ColumnTreatment.Unused, null, "No agreed use"),
        new(C.ExtendedField2, ColumnTreatment.Unused, null, "No agreed use"),
    ];

    /// <summary>True for columns the mapper must never fill.</summary>
    public static bool IsIntentionallyBlank(ColumnTreatment treatment) =>
        treatment is ColumnTreatment.Ambiguous or ColumnTreatment.NoSource or ColumnTreatment.Unused;
}
