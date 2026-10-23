using System.Text.Json.Nodes;
using OrisJewelCloudExport.Source;

namespace OrisJewelCloudExport.Tests;

public class SourceTableTests
{
    private static JsonObject Parse(string json) => JsonNode.Parse(json)!.AsObject();

    [Fact]
    public void Flattens_nested_objects_with_underscore()
    {
        var table = SourceTable.Build([Parse("""{"id":7,"price":{"currency":"USD","amount":"1195.00","active":false}}""")]);

        Assert.Equal(["SRC_id", "SRC_price_currency", "SRC_price_amount", "SRC_price_active"], table.Columns);
        Assert.Equal("7", table.Rows[0]["SRC_id"]);
        Assert.Equal("1195.00", table.Rows[0]["SRC_price_amount"]);
        Assert.Equal("false", table.Rows[0]["SRC_price_active"]);
    }

    [Fact]
    public void Preserves_properties_not_known_to_the_application()
    {
        var table = SourceTable.Build([
            Parse("""{"id":1,"name":"A"}"""),
            Parse("""{"id":2,"name":"B","waterResistance":"10 bar"}"""),
        ]);

        Assert.Equal(["SRC_id", "SRC_name", "SRC_waterResistance"], table.Columns);
        Assert.Equal("10 bar", table.Rows[1]["SRC_waterResistance"]);
        Assert.False(table.Rows[0].ContainsKey("SRC_waterResistance"));
    }

    [Fact]
    public void Null_object_leaves_child_cells_blank_without_an_extra_column()
    {
        var table = SourceTable.Build([
            Parse("""{"id":1,"price":{"amount":"10.00"}}"""),
            Parse("""{"id":2,"price":null}"""),
        ]);

        Assert.Equal(["SRC_id", "SRC_price_amount"], table.Columns);
        Assert.False(table.Rows[1].ContainsKey("SRC_price_amount"));
    }

    [Fact]
    public void Keeps_strings_verbatim_and_arrays_as_json()
    {
        var table = SourceTable.Build([Parse("""{"name":" BC4 \"Der Meisterflieger\"","size":"44.70 mm\r\n","tags":["Hölstein",1]}""")]);

        Assert.Equal(" BC4 \"Der Meisterflieger\"", table.Rows[0]["SRC_name"]);
        Assert.Equal("44.70 mm\r\n", table.Rows[0]["SRC_size"]);
        Assert.Equal("""["Hölstein",1]""", table.Rows[0]["SRC_tags"]);
    }
}
