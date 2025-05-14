using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AuctionService.Models;

public class Catalog
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }
    public string Name { get; set; }
    public DateTime AuctionDate { get; set; }
    public List<Listing> Listings { get; set; } = new();
}