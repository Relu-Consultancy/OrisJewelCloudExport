using OrisJewelCloudExport.Mapping;

namespace OrisJewelCloudExport.Tests;

public class OrisValueNormalizerTests
{
    [Theory]
    [InlineData("Automatic winding", "Automatic", "Mechanical")]
    [InlineData("Automatic winding, with red rotor", "Automatic", "Mechanical")]
    [InlineData("Automatic winding, uni-directionally rotating rotor", "Automatic", "Mechanical")]
    [InlineData("Hand winding", "Manual Mechanical", "Mechanical")]
    [InlineData("Quartz", null, null)]
    [InlineData(null, null, null)]
    public void Winding_maps_to_energy_and_movement(string? winding, string? energy, string? movement)
    {
        Assert.Equal(energy, OrisValueNormalizer.ToWatchEnergy(winding));
        Assert.Equal(movement, OrisValueNormalizer.ToWatchMovement(winding));
    }

    [Theory]
    [InlineData("Multi-piece stainless steel case", "stainless steel")]
    [InlineData("Multi-piece stainless steel case, rose gold PVD plated top ring", "stainless steel")]
    [InlineData("Multi-piece stainless steel case; bronze top ring", "stainless steel")]
    [InlineData("Multi-piece stainless steel case and black DLC plated", "stainless steel")]
    [InlineData("Multi-piece bronze case, stainless steel case back\r\n", "bronze")]
    [InlineData("Multi-piece stainless steel/bronze case, minutes scale top ring", "stainless steel/bronze")]
    [InlineData("Multi-piece 18K rose gold case, flexible lugs", "18k rose gold")]
    [InlineData("Multi-piece carbon fibre", "carbon fibre")]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void Extracts_case_metal_from_first_material_phrase(string? material, string? expected) =>
        Assert.Equal(expected, OrisValueNormalizer.ExtractCaseMetal(material));

    [Theory]
    [InlineData("stainless steel", "Stainless Steel")]
    [InlineData("titanium", "Titanium")]
    [InlineData("18k rose gold", "Rose Gold")]
    [InlineData("18k yellow gold", "Yellow Gold")]
    [InlineData("bronze", null)] // not in the JewelCloud list
    [InlineData("stainless steel/bronze", null)]
    public void Case_metal_maps_to_watch_case_material(string caseMetal, string? expected) =>
        Assert.Equal(expected, OrisValueNormalizer.ToWatchCaseMaterial(caseMetal));

    [Theory]
    [InlineData("Multi-piece stainless steel case, 18K rose gold top ring", "Steel: Stainless with Gold")]
    [InlineData("Multi-piece stainless steel case, rose gold PVD plated top ring", "Steel: Stainless")] // plating is not gold
    [InlineData("Multi-piece stainless steel case, rose gold plated top ring", "Steel: Stainless")]
    [InlineData("Multi-piece bronze case", "Bronze")]
    [InlineData("Multi-piece 18K yellow gold case", "18K Gold")]
    public void Metal_type_distinguishes_solid_gold_from_plating(string material, string expected) =>
        Assert.Equal(expected, OrisValueNormalizer.ToMetalType(OrisValueNormalizer.ExtractCaseMetal(material), material));

    [Theory]
    [InlineData("https://www.oris.ch/data/1614_01 754 7585 4164-07 4 25 01_verticale.png",
                "https://www.oris.ch/data/1614_01%20754%207585%204164-07%204%2025%2001_verticale.png")]
    [InlineData("https://www.oris.ch/data/a%20b.png", "https://www.oris.ch/data/a%20b.png")] // not double-encoded
    [InlineData("not a url", null)]
    [InlineData("ftp://example.com/a.png", null)]
    [InlineData("", null)]
    public void Image_urls_are_absolute_and_encoded(string input, string? expected) =>
        Assert.Equal(expected, OrisValueNormalizer.ToAbsoluteUrl(input));

    [Theory]
    [InlineData("1195.00", "1195.00")]
    [InlineData("2,800.00", "2800.00")]
    [InlineData("0.00", null)]
    [InlineData("-5", null)]
    [InlineData("n/a", null)]
    [InlineData(null, null)]
    public void Price_must_be_a_positive_number(string? amount, string? expected) =>
        Assert.Equal(expected, OrisValueNormalizer.ToPrice(amount));

    [Fact]
    public void CleanText_trims_trailing_line_breaks_and_blanks_to_null()
    {
        Assert.Equal("44.70 mm", OrisValueNormalizer.CleanText("44.70 mm\r\n"));
        Assert.Null(OrisValueNormalizer.CleanText("  \r\n"));
    }
}
