// AuctionMongoDBServiceTests.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AuctionService.Models;
using AuctionService.Services;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;
using MongoDB.Driver;

namespace AuctionServiceAPI.Tests
{
    [TestClass]
    public class AuctionMongoDBServiceTests
    {
        private Mock<ICatalogClient> mockCatalogClient;
        private Mock<ILogger<AuctionMongoDBService>> mockLogger;
        private Mock<IMongoCollection<Auction>> mockCollection;
        private Mock<IMongoDBContext> mockContext;
        private Mock<AuctionMongoDBService> serviceMock;
        private AuctionMongoDBService service;

        [TestInitialize]
        public void Setup()
        {
            mockCatalogClient = new Mock<ICatalogClient>();
            mockLogger = new Mock<ILogger<AuctionMongoDBService>>();
            mockCollection = new Mock<IMongoCollection<Auction>>();
            mockContext = new Mock<IMongoDBContext>();

            mockContext.Setup(c => c.Collection).Returns(mockCollection.Object);

            // partial mock to override FindByIdAsync
            serviceMock = new Mock<AuctionMongoDBService>(
                mockCatalogClient.Object,
                mockContext.Object,
                mockLogger.Object
            ) { CallBase = true };

            service = serviceMock.Object;
        }

        [TestMethod]
        public async Task CreateAuctionsFromCatalog_ShouldInsertAuctions_WhenCatalogExists()
        {
            var catalogId = Guid.NewGuid();
            var listings = new List<Listing> {
                new Listing { Id = Guid.NewGuid(), Name = "A" },
                new Listing { Id = Guid.NewGuid(), Name = "B" }
            };
            var catalog = new Catalog { Id = catalogId, Name = "C", Listings = listings };

            mockCatalogClient
                .Setup(c => c.GetCatalogAsync(catalogId))
                .ReturnsAsync(catalog);

            mockCollection
                .Setup(c => c.InsertManyAsync(
                    It.IsAny<IEnumerable<Auction>>(),
                    null,
                    default))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var result = await service.CreateAuctionsFromCatalog(catalogId);

            Assert.AreEqual(2, result.Count);
            mockCollection.Verify(
               c => c.InsertManyAsync(
                   It.IsAny<IEnumerable<Auction>>(),
                   null,
                   default),
               Times.Once);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception), "Catalog not found")]
        public async Task CreateAuctionsFromCatalog_ShouldThrow_WhenCatalogIsNull()
        {
            var catalogId = Guid.NewGuid();
            mockCatalogClient
                .Setup(c => c.GetCatalogAsync(catalogId))
                .ReturnsAsync((Catalog)null);

            await service.CreateAuctionsFromCatalog(catalogId);
        }

        [TestMethod]
        public async Task PlaceBidAsync_ShouldUpdateBid_WhenBidIsHigher()
        {
            var auctionId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var bidAmt = 42f;
            var req = new BidRequest { UserId = userId, BidAmount = bidAmt };

            // 1) UpdateOne succeeds
            mockCollection
                .Setup(c => c.UpdateOneAsync(
                    It.IsAny<FilterDefinition<Auction>>(),
                    It.IsAny<UpdateDefinition<Auction>>(),
                    default,
                    default))
                .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null))
                .Verifiable();

            // 2) Stub FindByIdAsync to return a filled Auction
            var updatedAuction = new Auction {
                Id = auctionId,
                ListingId = Guid.NewGuid(),
                ListingSnapshot = new Listing { Id = Guid.NewGuid(), Name = "X" },
                Bid = bidAmt,
                BidUserId = userId,
                BidHistory = new List<Bid> {
                    new Bid { UserId = userId, Amount = bidAmt, PlacedAt = DateTime.UtcNow }
                }
            };
            serviceMock
                .Protected()
                .Setup<Task<Auction>>("FindByIdAsync", auctionId)
                .ReturnsAsync(updatedAuction);

            var result = await service.PlaceBidAsync(auctionId, req);

            Assert.AreEqual(bidAmt, result.Bid);
            Assert.AreEqual(userId, result.BidUserId);
            Assert.AreEqual(1, result.BidHistory.Count);
            mockCollection.Verify();
        }

        [TestMethod]
        [ExpectedException(typeof(Exception), "Bid must be higher than the current bid.")]
        public async Task PlaceBidAsync_ShouldThrow_WhenBidIsLowerOrEqual()
        {
            var auctionId = Guid.NewGuid();
            var req = new BidRequest { UserId = Guid.NewGuid(), BidAmount = 1f };

            // UpdateOne reports 0 modified
            mockCollection
                .Setup(c => c.UpdateOneAsync(
                    It.IsAny<FilterDefinition<Auction>>(),
                    It.IsAny<UpdateDefinition<Auction>>(),
                    default,
                    default))
                .ReturnsAsync(new UpdateResult.Acknowledged(0, 0, null));

            await service.PlaceBidAsync(auctionId, req);
        }
    }
}
