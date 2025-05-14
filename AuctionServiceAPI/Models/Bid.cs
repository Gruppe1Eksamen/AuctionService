namespace AuctionService.Models;

public class Bid
{
    public Guid UserId { get; set; }
    public float Amount { get; set; }
    public DateTime PlacedAt { get; set; }
}

public class BidRequest
{
    public Guid UserId { get; set; }
    public float BidAmount { get; set; }
}