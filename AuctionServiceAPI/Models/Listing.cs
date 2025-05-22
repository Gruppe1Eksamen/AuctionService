using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace AuctionService.Models;

public enum ListingCategory
{
    None = 0,
    Furniture = 1,
    Jewellery = 2,
    Porcelain = 3,
    Gold = 4,
    Silver = 5,
    Painting = 6
}

public class Listing
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }
    public string Name { get; set; }
    public float AssesedPrice { get; set; }
    public string Description { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]

    public ListingCategory ListingCategory { get; set; }
    public string Location { get; set; }
    public Guid SellerId { get; set; }
    public List<Uri> Image { get; set; } = new List<Uri>();
}