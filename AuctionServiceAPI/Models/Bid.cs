namespace AuctionService.Models;

public class Bid
{
    public string UserId { get; set; }
    public float Amount { get; set; }
    public DateTime PlacedAt { get; set; }
}

public class BidRequest
{
    public string UserId { get; set; }
    public float BidAmount { get; set; }
}