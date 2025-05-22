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

            mockCollection
                .Setup(c => c.UpdateOneAsync(
                    It.IsAny<FilterDefinition<Auction>>(),
                    It.IsAny<UpdateDefinition<Auction>>(),
                    default,
                    default))
                .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null))
                .Verifiable();

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

            mockCollection
                .Setup(c => c.UpdateOneAsync(
                    It.IsAny<FilterDefinition<Auction>>(),
                    It.IsAny<UpdateDefinition<Auction>>(),
                    default,
                    default))
                .ReturnsAsync(new UpdateResult.Acknowledged(0, 0, null));

            await service.PlaceBidAsync(auctionId, req);
        }
        [TestMethod]
        public async Task OpenAuctionAsync_ShouldOpen_WhenPending()
        {
            var auctionId = Guid.NewGuid();
            var pending = new Auction { Id = auctionId, Status = AuctionStatus.Pending };
            var opened = new Auction { Id = auctionId, Status = AuctionStatus.Open };

            serviceMock
                .Protected()
                .SetupSequence<Task<Auction>>("FindByIdAsync", auctionId)
                .ReturnsAsync(pending)
                .ReturnsAsync(opened);

            mockCollection
                .Setup(c => c.UpdateOneAsync(
                    It.IsAny<FilterDefinition<Auction>>(),
                    It.IsAny<UpdateDefinition<Auction>>(),
                    default(UpdateOptions),
                    default(CancellationToken)))
                .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));

            var result = await service.OpenAuctionAsync(auctionId);
            Assert.AreEqual(AuctionStatus.Open, result.Status);
        }
        
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task OpenAuctionAsync_ShouldThrow_WhenNotPending()
        {
            var auctionId = Guid.NewGuid();
            var notPending = new Auction { Id = auctionId, Status = AuctionStatus.Open };
            serviceMock
                .Protected()
                .Setup<Task<Auction>>("FindByIdAsync", auctionId)
                .ReturnsAsync(notPending);

            await service.OpenAuctionAsync(auctionId);
        }
        
        [TestMethod]
        public async Task CloseAuctionAsync_ShouldClose_WhenOpen()
        {
            var auctionId = Guid.NewGuid();
            var open = new Auction { Id = auctionId, Status = AuctionStatus.Open, Bid = 10, BidUserId = Guid.NewGuid() };
            var closed = new Auction { Id = auctionId, Status = AuctionStatus.Closed, Bid = 10, BidUserId = open.BidUserId, WinnerUserId = open.BidUserId, WinningBid = 10 };

            serviceMock
                .Protected()
                .SetupSequence<Task<Auction>>("FindByIdAsync", auctionId)
                .ReturnsAsync(open)
                .ReturnsAsync(closed);

            mockCollection
                .Setup(c => c.UpdateOneAsync(
                    It.IsAny<FilterDefinition<Auction>>(),
                    It.IsAny<UpdateDefinition<Auction>>(),
                    default(UpdateOptions),
                    default(CancellationToken)))
                .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));

            var result = await service.CloseAuctionAsync(auctionId);
            Assert.AreEqual(AuctionStatus.Closed, result.Status);
            Assert.AreEqual(open.BidUserId, result.WinnerUserId);
        }
        
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task CloseAuctionAsync_ShouldThrow_WhenNotOpen()
        {
            var auctionId = Guid.NewGuid();
            var notOpen = new Auction { Id = auctionId, Status = AuctionStatus.Pending };
            serviceMock
                .Protected()
                .Setup<Task<Auction>>("FindByIdAsync", auctionId)
                .ReturnsAsync(notOpen);

            await service.CloseAuctionAsync(auctionId);
        }
        
        [TestMethod]
        public async Task GetAuctionWinnerAsync_ShouldReturnWinner_WhenClosed()
        {
            var auctionId = Guid.NewGuid();
            var closed = new Auction { Id = auctionId, Status = AuctionStatus.Closed, WinnerUserId = Guid.NewGuid(), WinningBid = 20 };
            serviceMock
                .Protected()
                .Setup<Task<Auction>>("FindByIdAsync", auctionId)
                .ReturnsAsync(closed);

            var dto = await service.GetAuctionWinnerAsync(auctionId);
            Assert.AreEqual(auctionId, dto.AuctionId);
            Assert.AreEqual(closed.WinnerUserId, dto.WinnerUserId);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task GetAuctionWinnerAsync_ShouldThrow_WhenNotClosed()
        {
            var auctionId = Guid.NewGuid();
            var open = new Auction { Id = auctionId, Status = AuctionStatus.Open };
            serviceMock
                .Protected()
                .Setup<Task<Auction>>("FindByIdAsync", auctionId)
                .ReturnsAsync(open);

            await service.GetAuctionWinnerAsync(auctionId);
        }

        [TestMethod]
        public async Task UpdatePickUpAsync_ShouldSetPickedUp_WhenExists()
        {
            var auctionId = Guid.NewGuid();
            var updatedAuction = new Auction { Id = auctionId, PickedUp = true };
            mockCollection
                .Setup(c => c.FindOneAndUpdateAsync(
                    It.IsAny<FilterDefinition<Auction>>(),
                    It.IsAny<UpdateDefinition<Auction>>(),
                    It.IsAny<FindOneAndUpdateOptions<Auction>>(),
                    default(CancellationToken)))
                .ReturnsAsync(updatedAuction);

            var result = await service.UpdatePickUpAsync(auctionId);
            Assert.IsTrue(result.PickedUp);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task UpdatePickUpAsync_ShouldThrow_WhenNotFound()
        {
            var auctionId = Guid.NewGuid();
            mockCollection
                .Setup(c => c.FindOneAndUpdateAsync(
                    It.IsAny<FilterDefinition<Auction>>(),
                    It.IsAny<UpdateDefinition<Auction>>(),
                    It.IsAny<FindOneAndUpdateOptions<Auction>>(),
                    default(CancellationToken)))
                .ReturnsAsync((Auction)null);

            await service.UpdatePickUpAsync(auctionId);
        }
        
    }
}
