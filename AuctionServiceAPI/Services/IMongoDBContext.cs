using AuctionService.Models;
using MongoDB.Driver;

namespace AuctionService.Services;

public interface IMongoDBContext
{
    IMongoCollection<Auction> Collection { get; }
}