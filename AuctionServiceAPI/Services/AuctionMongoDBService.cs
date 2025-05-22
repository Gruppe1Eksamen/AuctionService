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
                //  finder auction
Builders<Auction>.Filter.Eq(a => a.Id, auctionId),

                // tjekker at den er åben
                Builders<Auction>.Filter.Eq(a => a.Status, AuctionStatus.Open),

                // sikrer at nyt bud er højere
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
        
        public async Task<Auction> CloseAuctionAsync(Guid auctionId)
        {
            // 1) Fetch the existing auction
            var existing = await FindByIdAsync(auctionId)
                           ?? throw new InvalidOperationException("Not found");

            if (existing.Status != AuctionStatus.Open)
                throw new InvalidOperationException("Not open or already closed");

            // 2) Build your filter to ensure you only close an open auction
            var filter = Builders<Auction>.Filter.And(
                Builders<Auction>.Filter.Eq(a => a.Id, auctionId),
                Builders<Auction>.Filter.Eq(a => a.Status, AuctionStatus.Open)
            );

            // 3) Use the fetched values in your update
            var update = Builders<Auction>.Update
                .Set(a => a.Status, AuctionStatus.Closed)
                .Set(a => a.WinnerUserId, existing.BidUserId)
                .Set(a => a.WinningBid,   existing.Bid);

            var result = await _context.Collection.UpdateOneAsync(filter, update);
            if (result.ModifiedCount == 0)
                throw new InvalidOperationException("Failed to close (maybe already closed)");

            // 4) Return the now‐closed auction
            return await FindByIdAsync(auctionId) 
                   ?? throw new InvalidOperationException("Just closed but now missing?");
        }

        public async Task<Auction> OpenAuctionAsync(Guid auctionId)
        {
            // 1) Fetch and validate current state
            var existing = await FindByIdAsync(auctionId)
                           ?? throw new InvalidOperationException("Auction not found");
            if (existing.Status != AuctionStatus.Pending)
                throw new InvalidOperationException("Only pending auctions can be opened");

            // 2) Build an atomic filter: matching Id + Pending status
            var filter = Builders<Auction>.Filter.And(
                Builders<Auction>.Filter.Eq(a => a.Id, auctionId),
                Builders<Auction>.Filter.Eq(a => a.Status, AuctionStatus.Pending)
            );

            // 3) Update only the status → Open
            var update = Builders<Auction>.Update
                .Set(a => a.Status, AuctionStatus.Open);

            var result = await _context.Collection.UpdateOneAsync(filter, update);
            if (result.ModifiedCount == 0)
                throw new InvalidOperationException("Failed to open auction (maybe already open)");

            // 4) Return fresh document
            return await FindByIdAsync(auctionId)
                   ?? throw new InvalidOperationException("Auction disappeared after opening");
        }
        
        public async Task<AuctionWinnerDto> GetAuctionWinnerAsync(Guid auctionId)
        {
            var auction = await FindByIdAsync(auctionId)
                          ?? throw new KeyNotFoundException($"Auction {auctionId} not found");

            if (auction.Status != AuctionStatus.Closed)
                throw new InvalidOperationException("Auction must be closed before querying the winner");

            return new AuctionWinnerDto {
                AuctionId     = auction.Id,
                WinnerUserId  = auction.WinnerUserId,
                WinningBid    = auction.WinningBid
            };
        }

        public async Task<Auction> UpdatePickUpAsync(Guid auctionId)
        {
            // 1) Build a filter matching the auction by its Id
            var filter = Builders<Auction>.Filter.Eq(a => a.Id, auctionId);

            // 2) Define the update to set PickedUp = true
            var update = Builders<Auction>.Update.Set(a => a.PickedUp, true);

            // 3) Ask Mongo to return the document _after_ the update
            var options = new FindOneAndUpdateOptions<Auction>
            {
                ReturnDocument = ReturnDocument.After
            };

            // 4) Perform the atomic find-and-update on the correct collection
            var updated = await _context
                .Collection
                .FindOneAndUpdateAsync(filter, update, options);

            // 5) If nothing was matched/updated, no auction with that Id existed
            if (updated == null)
                throw new InvalidOperationException($"Auction {auctionId} not found");

            return updated;
        }



//hjælpemedtode
        protected virtual Task<Auction?> FindByIdAsync(Guid auctionId)
        {
            return _context
                .Collection
                .Find(a => a.Id == auctionId)
                .FirstOrDefaultAsync();
        }
    }
    
    
}
