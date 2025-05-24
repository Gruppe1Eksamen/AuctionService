using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AuctionService.Models;
using AuctionService.Services;
using AuctionServiceAPI.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace AuctionServiceAPI.Tests
{
    [TestClass]
    public class AuctionControllerTests
    {
        private Mock<ILogger<AuctionController>> _mockLogger;
        private Mock<IConfiguration> _mockConfig;
        private Mock<IAuctionMongoDBService> _mockService;
        private HttpClient _httpClient;
        private AuctionController _controller;
        private Auction _sampleAuction;
        private List<Listing> _sampleListings;

        [TestInitialize]
        public void Setup()
        {
            _mockLogger = new Mock<ILogger<AuctionController>>();
            _mockConfig = new Mock<IConfiguration>();
            _mockConfig.SetupGet(c => c["LISTINGSERVICE_ENDPOINT"]).Returns("http://fake");

            _mockService = new Mock<IAuctionMongoDBService>();
            
            _controller = new AuctionController(
                _mockLogger.Object,
                _mockConfig.Object,
                _httpClient,
                _mockService.Object
            );

            _sampleAuction = new Auction
            {
                Id = "A1",
                ListingId = "L1",
                Bid = 100f,
                BidUserId = "user1",
                EndsAt = DateTime.UtcNow.AddHours(1),
                Status = AuctionStatus.Open,
                BidHistory = new List<Bid>
                {
                    new Bid { UserId = "user1", Amount = 100f, PlacedAt = DateTime.UtcNow }
                },
                WinnerUserId = null,
                WinningBid = null,
                PickedUp = false,
                AuctionDate = DateTime.UtcNow
            };

            _sampleListings = new List<Listing>
            {
                new Listing
                {
                    Id = "L1",
                    Name = "Item1",
                    AssesedPrice = 200f,
                    Description = "Desc",
                    ListingCategory = ListingCategory.Furniture,
                    Location = "Cph",
                    SellerId = "s1"
                }
            };
        }

        [TestMethod]
        public async Task GetAllAuctions_WithStatus_ReturnsOkList()
        {
            // Arrange
            var list = new List<Auction> { _sampleAuction };
            _mockService
                .Setup(s => s.GetAuctionsByStatusAsync(AuctionStatus.Open))
                .ReturnsAsync(list);

            // Act
            var result = await _controller.GetAllAuctions(AuctionStatus.Open) as OkObjectResult;

            // Assert
            Assert.IsNotNull(result);
            var returned = result.Value as List<Auction>;
            Assert.IsNotNull(returned);
            CollectionAssert.AreEqual(list, returned);
        }

        [TestMethod]
        public async Task PlaceBid_Success_ReturnsOk()
        {
            // Arrange
            var req = new BidRequest { UserId = "u", BidAmount = 123f };
            _mockService
                .Setup(s => s.PlaceBidAsync("A1", req))
                .ReturnsAsync(_sampleAuction);

            // Act
            var result = await _controller.PlaceBid("A1", req) as OkObjectResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(_sampleAuction, result.Value);
        }

        [TestMethod]
        public async Task Open_Success_ReturnsOpenedAuction()
        {
            // Arrange
            _sampleAuction.Status = AuctionStatus.Open;
            _mockService
                .Setup(s => s.OpenAuctionAsync("A1"))
                .ReturnsAsync(_sampleAuction);

            // Act
            var result = await _controller.Open("A1") as OkObjectResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result.Value, typeof(Auction));
            var returned = (Auction)result.Value!;
            Assert.AreEqual(AuctionStatus.Open, returned.Status);
        }

        [TestMethod]
        public async Task Close_Success_ReturnsOkAuction()
        {
            // Arrange
            var closedAuction = _sampleAuction;
            closedAuction.Status = AuctionStatus.Closed;
            _mockService
                .Setup(s => s.CloseAuctionAsync("A1"))
                .ReturnsAsync(closedAuction);

            // Act
            var result = await _controller.Close("A1") as OkObjectResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(closedAuction, result.Value);
        }

        [TestMethod]
        public async Task GetWinner_Success_ReturnsOkDto()
        {
            // Arrange
            var dto = new AuctionWinnerDto { AuctionId = "A1", WinnerUserId = "u", WinningBid = 123f };
            _mockService
                .Setup(s => s.GetAuctionWinnerAsync("A1"))
                .ReturnsAsync(dto);

            // Act
            var result = await _controller.GetWinner("A1") as OkObjectResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(dto, result.Value);
        }

        [TestMethod]
        public async Task PickUp_Success_ReturnsOkAuction()
        {
            // Arrange
            _mockService
                .Setup(s => s.UpdatePickUpAsync("A1"))
                .ReturnsAsync(_sampleAuction);

            // Act
            var result = await _controller.PickUp("A1") as OkObjectResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(_sampleAuction, result.Value);
        }
    }
}
