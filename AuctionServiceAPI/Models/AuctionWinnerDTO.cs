namespace AuctionService.Models;

public class AuctionWinnerDto
{
    public string AuctionId      { get; set; }
    public string? WinnerUserId  { get; set; }
    public float? WinningBid   { get; set; }
}