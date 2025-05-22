using AuctionService.Models;

namespace AuctionService.Services;

public interface IListingClient
{
    Task<List<Listing>> GetAllListingsAsync();
}