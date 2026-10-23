using System.Text.Json.Nodes;

namespace OrisJewelCloudExport.Source;

/// <summary>
/// Typed view of the Oris fields that the JewelCloud mapping uses. Every field is nullable because the
/// API guarantees none of them. Fields not listed here are not lost: the SRC_ columns are built from
/// the raw JSON, not from this type.
/// </summary>
public sealed record OrisWatch
{
    public string? Reference { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? Collection { get; init; }
    public string? Material { get; init; }
    public string? Winding { get; init; }
    public string? ImageUrl { get; init; }
    public string? Image2Url { get; init; }
    public bool? IsArchived { get; init; }
    public OrisPrice? Price { get; init; }

    public static OrisWatch FromJson(JsonObject json) => new()
    {
        Reference = json.GetString("reference"),
        Name = json.GetString("name"),
        Description = json.GetString("description"),
        Collection = json.GetString("collection"),
        Material = json.GetString("material"),
        Winding = json.GetString("winding"),
        ImageUrl = json.GetString("imageUrl"),
        Image2Url = json.GetString("image2Url"),
        IsArchived = json.GetBoolean("isArchived"),
        Price = json["price"] is JsonObject price ? OrisPrice.FromJson(price) : null,
    };
}

public sealed record OrisPrice(string? PriceType, string? Currency, string? Amount, bool? Active)
{
    public static OrisPrice FromJson(JsonObject json) => new(
        json.GetString("priceType"),
        json.GetString("currency"),
        json.GetString("amount"),
        json.GetBoolean("active"));
}
