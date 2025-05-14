using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuctionService.Models;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace AuctionService.Services
{
    public class AuctionMongoDBService : IAuctionMongoDBService
    {
        private readonly ICatalogClient _catalogClient;
        private readonly IMongoDBContext _context;
        private readonly ILogger<AuctionMongoDBService> _logger;

        public AuctionMongoDBService(
            ICatalogClient catalogClient,
            IMongoDBContext context,
            ILogger<AuctionMongoDBService> logger)
        {
            _catalogClient = catalogClient;
            _context = context;
            _logger = logger;
        }

        public async Task<List<Auction>> CreateAuctionsFromCatalog(Guid catalogId)
        {
            var catalog = await _catalogClient.GetCatalogAsync(catalogId);
            if (catalog == null)
            {
                _logger.LogWarning("Catalog {CatalogId} not found", catalogId);
                throw new Exception("Catalog not found");
            }

            var auctions = catalog.Listings
                .Select(listing => new Auction
                {
                    Id = Guid.NewGuid(),
                    ListingId = listing.Id,
                    ListingSnapshot = listing,
                    Bid = 0,
                    BidUserId = Guid.Empty
                })
                .ToList();

            await _context.Collection.InsertManyAsync(auctions);
            _logger.LogInformation(
                "Inserted {Count} auctions from catalog {CatalogId}",
                auctions.Count, catalogId);

            return auctions;
        }

        public async Task<Auction> PlaceBidAsync(Guid auctionId, BidRequest bid)
        {
            var filter = Builders<Auction>.Filter.And(
                Builders<Auction>.Filter.Eq(a => a.Id, auctionId),
                Builders<Auction>.Filter.Lt(a => a.Bid, bid.BidAmount)
            );

            var bidEntry = new Bid
            {
                UserId = bid.UserId,
                Amount = bid.BidAmount,
                PlacedAt = DateTime.UtcNow
            };

            var update = Builders<Auction>.Update
                .Set(a => a.Bid, bid.BidAmount)
                .Set(a => a.BidUserId, bid.UserId)
                .Push(a => a.BidHistory, bidEntry);

            var result = await _context.Collection.UpdateOneAsync(filter, update);
            if (result.ModifiedCount == 0)
                throw new Exception("Bid must be higher than the current bid.");

            // use our helper instead of calling the extension directly
            var updatedAuction = await FindByIdAsync(auctionId);
            return updatedAuction!;
        }

        // NEW helper to wrap the extension method so tests can mock it
        protected virtual Task<Auction?> FindByIdAsync(Guid auctionId)
        {
            return _context
                .Collection
                .Find(a => a.Id == auctionId)
                .FirstOrDefaultAsync();
        }
    }
}
