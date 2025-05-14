using AuctionService.Models;

namespace AuctionService.Services;

public interface IAuctionMongoDBService
{
    Task<List<Auction>> CreateAuctionsFromCatalog(Guid catalogId);

    Task<Auction> PlaceBidAsync(Guid auctionId, BidRequest bid);
}