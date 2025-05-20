namespace AuctionService.Models;

public class AuctionWinnerDto
{
    public Guid AuctionId      { get; set; }
    public Guid? WinnerUserId  { get; set; }
    public float? WinningBid   { get; set; }
}