using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using AuctionService.Models;

namespace AuctionService.Models;

public class Auction
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }
    
    public Guid ListingId { get; set; }
    public Listing ListingSnapshot { get; set; }
    
    public float Bid { get; set; }
    public Guid BidUserId { get; set; }

    public List<Bid> BidHistory { get; set; }
    
}