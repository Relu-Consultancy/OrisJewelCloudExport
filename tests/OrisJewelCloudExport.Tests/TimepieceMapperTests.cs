using System.Text.Json.Nodes;
using OrisJewelCloudExport.Configuration;
using OrisJewelCloudExport.Mapping;
using OrisJewelCloudExport.Source;
using C = OrisJewelCloudExport.Mapping.TimepieceColumns;

namespace OrisJewelCloudExport.Tests;

public class TimepieceMapperTests
{
    // The sample record from the assessment brief.
    private const string SampleJson = """
        {
          "id": 7, "familyId": 3, "reference": "01 754 7585 4164-07 4 25 01",
          "imageUrl": "https://www.oris.ch/data/1614_01 754 7585 4164-07 4 25 01_verticale.png",
          "image2Url": "https://www.oris.ch/data/21483_01 754 7585 4164-07 4 25 01_horizontale.png",
          "family": "WilliamsF1 Team Pointer Date", "calibre": "754", "winding": "Automatic winding",
          "powerReserve": "38 hrs", "collection": "Williams",
          "price": { "priceType": "US_USD", "currency": "USD", "amount": "1195.00", "active": false },
          "description": "Williams, 42.00 mm, Multi-piece stainless steel case, grad scale top ring, flexible lugs",
          "isArchived": true, "hasStock": false,
          "productLink": "https://www.oris.ch/en-US/product/watch/williams/williamsf1-team-pointer-date/01-754-7585-4164-07-4-25-01",
          "material": "Multi-piece stainless steel case, grad scale top ring, flexible lugs",
          "color": "Black", "size": "42.00 mm", "name": "WilliamsF1 Team Pointer Date", "limitedEdition": false
        }
        """;

    private static IReadOnlyDictionary<string, string?> Map(string json, bool includeInactivePrices = false) =>
        new TimepieceMapper(new ExportSettings { IncludeInactivePrices = includeInactivePrices }, new MappingDiagnostics())
            .Map(OrisWatch.FromJson(JsonNode.Parse(json)!.AsObject()));

    [Fact]
    public void Maps_the_brief_sample_record()
    {
        var row = Map(SampleJson);

        Assert.Equal("1", row[C.IsActive]); // archived
        Assert.Equal("0", row[C.ProductType]);
        Assert.Equal("01 754 7585 4164-07 4 25 01", row[C.StyleNumber]);
        Assert.Equal(row[C.StyleNumber], row[C.RetailerStockNumber]);
        Assert.Equal("WilliamsF1 Team Pointer Date", row[C.ProductName]);
        Assert.Equal("Williams", row[C.Collections]);
        Assert.Equal("Watches", row[C.Categories]);
        Assert.Equal("Steel: Stainless", row[C.MetalType]);
        Assert.Equal("Stainless Steel", row[C.WatchCaseMaterial]);
        Assert.Equal("Automatic", row[C.WatchEnergy]);
        Assert.Equal("Mechanical", row[C.WatchMovement]);
        Assert.StartsWith("https://www.oris.ch/data/1614_01%20754", row[C.ImagePath]);
        Assert.Null(row[C.RetailPrice]); // price.active = false
    }

    [Fact]
    public void Never_fills_columns_the_catalog_marks_as_intentionally_blank()
    {
        var row = Map(SampleJson);

        var alwaysBlank = TimepieceColumnCatalog.All
            .Where(c => TimepieceColumnCatalog.IsIntentionallyBlank(c.Treatment))
            .Select(c => c.Name)
            .ToList();

        Assert.Contains(C.WatchDialColor, alwaysBlank);
        Assert.Contains(C.WatchSize, alwaysBlank);
        Assert.All(alwaysBlank, column => Assert.Null(row.GetValueOrDefault(column)));
    }

    [Fact]
    public void Catalog_lists_every_template_column_in_order() =>
        Assert.Equal(C.All, TimepieceColumnCatalog.All.Select(c => c.Name));

    [Fact]
    public void Inactive_price_is_used_only_when_configured()
    {
        Assert.Equal("1195.00", Map(SampleJson, includeInactivePrices: true)[C.RetailPrice]);
    }

    [Fact]
    public void Missing_and_null_values_produce_blanks_not_errors()
    {
        var row = Map("""{"reference":"X1","price":null,"winding":null}""");

        Assert.Equal("X1", row[C.StyleNumber]);
        Assert.Null(row[C.IsActive]);
        Assert.Null(row[C.RetailPrice]);
        Assert.Null(row[C.WatchEnergy]);
        Assert.Null(row[C.MetalType]);
    }

    [Fact]
    public void Only_template_columns_are_produced()
    {
        Assert.All(Map(SampleJson).Keys, key => Assert.Contains(key, C.All));
    }
}
