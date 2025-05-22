using AuctionService.Models;

namespace AuctionService.Services;

public interface IAuctionMongoDBService
{
    Task<List<Auction>> CreateAuctionsFromListingsAsync();
    Task<Auction> PlaceBidAsync(Guid auctionId, BidRequest bid);
    
    Task<Auction> CloseAuctionAsync(Guid auctionId);

    Task<Auction> OpenAuctionAsync(Guid auctionId);
    
    Task<AuctionWinnerDto> GetAuctionWinnerAsync(Guid auctionId);

    Task<Auction> UpdatePickUpAsync(Guid auctionId);
    
    Task<List<Auction>> GetAuctionsByStatusAsync(AuctionStatus? status);



}