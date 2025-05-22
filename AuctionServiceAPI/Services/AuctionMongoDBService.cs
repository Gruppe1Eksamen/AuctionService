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
        private readonly IListingClient _listingClient;
        private readonly IMongoDBContext _context;
        private readonly ILogger<AuctionMongoDBService> _logger;

        public AuctionMongoDBService(
            IListingClient listingClient,
            IMongoDBContext context,
            ILogger<AuctionMongoDBService> logger)
        {
            _listingClient = listingClient;
            _context = context;
            _logger = logger;
        }

        public async Task<List<Auction>> CreateAuctionsFromListingsAsync()
        {
            //henter listings
            var listings = await _listingClient.GetAllListingsAsync();
            if (listings == null || listings.Count == 0)
            {
                _logger.LogWarning("No listings returned from ListingClient");
                return new List<Auction>();
            }

            var created = new List<Auction>();

            foreach (var listing in listings)
            {
                //hvis de allerede er auctioner, blive de skippet
                var exists = await _context
                    .Collection
                    .Find(a => a.ListingId == listing.Id)
                    .AnyAsync();

                if (exists)
                {
                    _logger.LogDebug("Skipping listing {ListingId}, auction already exists.", listing.Id);
                    continue;
                }

                //opretter nye pending auctions
                var auction = new Auction
                {
                    Id              = Guid.NewGuid(),
                    ListingId       = listing.Id,
                    ListingSnapshot = listing,
                    Status          = AuctionStatus.Pending,
                    Bid             = 0,
                    BidUserId       = Guid.Empty,
                    BidHistory      = new List<Bid>(),
                    AuctionDate     = DateTime.UtcNow
                };

                // indsætter dem i collection
                await _context.Collection.InsertOneAsync(auction);
                created.Add(auction);
            }

            _logger.LogInformation("Inserted {Count} new auctions from listings.", created.Count);
            return created;
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
        public async Task<List<Auction>> GetAuctionsByStatusAsync(AuctionStatus? status)
        {
            var filter = status.HasValue
                ? Builders<Auction>.Filter.Eq(a => a.Status, status.Value)
                : Builders<Auction>.Filter.Empty;

            return await _context
                .Collection
                .Find(filter)
                .ToListAsync();
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
