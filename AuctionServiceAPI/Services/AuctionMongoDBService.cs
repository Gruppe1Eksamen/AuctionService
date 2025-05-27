using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuctionService.Models;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
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
                    Id = ObjectId.GenerateNewId().ToString(),
                    ListingId       = listing.Id,
                    ListingSnapshot = listing,
                    Status          = AuctionStatus.Pending,
                    Bid             = 0,
                    BidUserId       = null,
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

        public async Task<Auction> PlaceBidAsync(string auctionId, BidRequest bid)
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
                throw new Exception("Unable to place bid.");

            // use our helper instead of calling the extension directly
            var updatedAuction = await FindByIdAsync(auctionId);
            return updatedAuction!;
        }
        
        public async Task<Auction> CloseAuctionAsync(string auctionId)
        {
            // finder auktionen
            var existing = await FindByIdAsync(auctionId)
                           ?? throw new InvalidOperationException("Not found");

            if (existing.Status != AuctionStatus.Open)
                throw new InvalidOperationException("Not open or already closed");

            // sikrer os at auktionen er åben, for at vi kan lukke den
            var filter = Builders<Auction>.Filter.And(
                Builders<Auction>.Filter.Eq(a => a.Id, auctionId),
                Builders<Auction>.Filter.Eq(a => a.Status, AuctionStatus.Open)
            );

            // tildeler værdier for winning user/bid
            var update = Builders<Auction>.Update
                .Set(a => a.Status, AuctionStatus.Closed)
                .Set(a => a.WinnerUserId, existing.BidUserId)
                .Set(a => a.WinningBid,   existing.Bid);

            var result = await _context.Collection.UpdateOneAsync(filter, update);
            if (result.ModifiedCount == 0)
                throw new InvalidOperationException("Failed to close (maybe already closed)");

            // returnerer den nu lukkede auktion
            return await FindByIdAsync(auctionId) 
                   ?? throw new InvalidOperationException("Just closed but now missing?");
        }

        public async Task<Auction> OpenAuctionAsync(string auctionId)
        {
            // validerer den nuværende state
            var existing = await FindByIdAsync(auctionId)
                           ?? throw new InvalidOperationException("Auction not found");
            if (existing.Status != AuctionStatus.Pending)
                throw new InvalidOperationException("Only pending auctions can be opened");

            // finder auktionen og tjekker at den er pending
            var filter = Builders<Auction>.Filter.And(
                Builders<Auction>.Filter.Eq(a => a.Id, auctionId),
                Builders<Auction>.Filter.Eq(a => a.Status, AuctionStatus.Pending)
            );

            // opdaterer state til open
            var update = Builders<Auction>.Update
                .Set(a => a.Status, AuctionStatus.Open);

            var result = await _context.Collection.UpdateOneAsync(filter, update);
            if (result.ModifiedCount == 0)
                throw new InvalidOperationException("Failed to open auction (maybe already open)");

            // retunerer auktionen
            return await FindByIdAsync(auctionId)
                   ?? throw new InvalidOperationException("Auction disappeared after opening");
        }
        
        public async Task<AuctionWinnerDto> GetAuctionWinnerAsync(string auctionId)
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

        public async Task<Auction> UpdatePickUpAsync(string auctionId)
        {
            // finder auktionen
            var filter = Builders<Auction>.Filter.Eq(a => a.Id, auctionId);

            // definerer opdateringen for værdien for pickedup
            var update = Builders<Auction>.Update.Set(a => a.PickedUp, true);

            var options = new FindOneAndUpdateOptions<Auction>
            {
                ReturnDocument = ReturnDocument.After
            };

            // opdaterer væriden
            var updated = await _context
                .Collection
                .FindOneAndUpdateAsync(filter, update, options);

            // hvis der ikke blev opdateret noget, blev auktionen ikke fundet
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
        
        public async Task<List<Auction>> GetAuctionsByWinnerId(string winnerId)
        {

            var filter = Builders<Auction>.Filter.And(
                Builders<Auction>.Filter.Eq(a => a.Status, AuctionStatus.Closed),
                Builders<Auction>.Filter.Eq(a => a.WinnerUserId, winnerId)
            );

            return await _context
                .Collection
                .Find(filter)
                .ToListAsync();
        }
    




//hjælpemedtode
        protected virtual Task<Auction?> FindByIdAsync(string auctionId)
        {
            return _context
                .Collection
                .Find(a => a.Id == auctionId)
                .FirstOrDefaultAsync();
        }
        
        public async Task<List<Listing>> ReturnAllListings()
        {
            //henter listings
            var listings = await _listingClient.GetAllListingsAsync();
            if (listings == null || listings.Count == 0)
            {
                _logger.LogWarning("No listings returned from ListingClient");
                return new List<Listing>();
            }

            return listings;
        }
    }
    
    
    
    
}
