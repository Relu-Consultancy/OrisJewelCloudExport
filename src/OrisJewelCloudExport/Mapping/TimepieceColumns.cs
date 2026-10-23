namespace OrisJewelCloudExport.Mapping;

/// <summary>Columns of the JewelCloud Simple Product "Timepieces" sheet, in template order.</summary>
public static class TimepieceColumns
{
    public const string IsActive = "IsActive";
    public const string ProductType = "ProductType";
    public const string StyleNumber = "StyleNumber";
    public const string RetailerStockNumber = "RetailerStockNumber";
    public const string ProductName = "ProductName";
    public const string ProductDescription = "ProductDescription";
    public const string MetalType = "MetalType";
    public const string MetalColor = "MetalColor";
    public const string ImagePath = "ImagePath";
    public const string Collections = "Collections";
    public const string Categories = "Categories";
    public const string WatchBandMaterial = "WatchBandMaterial";
    public const string WatchBandType = "WatchBandType";
    public const string WatchCaseMaterial = "WatchCaseMaterial";
    public const string WatchCaseShape = "WatchCaseShape";
    public const string WatchCrystalType = "WatchCrystalType";
    public const string WatchDialColor = "WatchDialColor";
    public const string WatchDisplayType = "WatchDisplayType";
    public const string WatchMovement = "WatchMovement";
    public const string WatchNumberType = "WatchNumberType";
    public const string WatchEnergy = "WatchEnergy";
    public const string Gender = "Gender";
    public const string PriceType = "PriceType";
    public const string DeliveryTime = "DeliveryTime";
    public const string WatchSize = "WatchSize";
    public const string WatchType = "WatchType";
    public const string ImagePath2 = "ImagePath2";
    public const string RetailPrice = "RetailPrice";
    public const string ExtendedField1 = "ExtendedField1";
    public const string ExtendedField2 = "ExtendedField2";

    public static readonly IReadOnlyList<string> All =
    [
        IsActive, ProductType, StyleNumber, RetailerStockNumber, ProductName, ProductDescription,
        MetalType, MetalColor, ImagePath, Collections, Categories, WatchBandMaterial, WatchBandType,
        WatchCaseMaterial, WatchCaseShape, WatchCrystalType, WatchDialColor, WatchDisplayType,
        WatchMovement, WatchNumberType, WatchEnergy, Gender, PriceType, DeliveryTime, WatchSize,
        WatchType, ImagePath2, RetailPrice, ExtendedField1, ExtendedField2,
    ];
}
