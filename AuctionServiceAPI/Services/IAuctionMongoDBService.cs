using AuctionService.Models;

namespace AuctionService.Services;

public interface IAuctionMongoDBService
{
    Task<List<Auction>> CreateAuctionsFromListingsAsync();
    Task<Auction> PlaceBidAsync(string auctionId, BidRequest bid);
    
    Task<Auction> CloseAuctionAsync(string auctionId);

    Task<Auction> OpenAuctionAsync(string auctionId);
    
    Task<AuctionWinnerDto> GetAuctionWinnerAsync(string auctionId);

    Task<Auction> UpdatePickUpAsync(string auctionId);
    
    Task<List<Auction>> GetAuctionsByStatusAsync(AuctionStatus? status);

    Task<List<Listing>> ReturnAllListings();
    
    Task<List<Auction>> GetAuctionsByWinnerId(string winnerId);





}