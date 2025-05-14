using AuctionService.Models;

namespace AuctionService.Services;

public interface ICatalogClient
{
    Task<Catalog?> GetCatalogAsync(Guid catalogId);

}