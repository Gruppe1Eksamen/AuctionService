﻿using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using AuctionService.Models;

namespace AuctionService.Models;

public class Auction
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; }
    
    public string ListingId { get; set; }
    
    public Listing ListingSnapshot { get; set; }
    
    public float Bid { get; set; }
    
    public string BidUserId { get; set; }
    
    public DateTime? EndsAt { get; set; }
    
    public AuctionStatus Status { get; set; } = AuctionStatus.Pending;

    public List<Bid> BidHistory { get; set; }
    
    public string? WinnerUserId { get; set; }
    
    public float? WinningBid { get; set; }

    public bool PickedUp { get; set; }
    
    public DateTime AuctionDate { get; set; }
    
}
public enum AuctionStatus { Pending, Open, Closed }
