using AuctionService.Models;

namespace AuctionService.Services;

public interface IAuctionMongoDBService
{
    Task<List<Auction>> CreateAuctionsFromCatalog(Guid catalogId);

    Task<Auction> PlaceBidAsync(Guid auctionId, BidRequest bid);
    
    Task<Auction> CloseAuctionAsync(Guid auctionId);

    Task<Auction> OpenAuctionAsync(Guid auctionId);
    
    Task<AuctionWinnerDto> GetAuctionWinnerAsync(Guid auctionId);

    Task<Auction> UpdatePickUpAsync(Guid auctionId);


}