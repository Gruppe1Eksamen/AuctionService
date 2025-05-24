using AuctionService.Models;
using AuctionService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuctionServiceAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class AuctionController : ControllerBase
{
    private readonly IAuctionMongoDBService _auctionService;
    //tilføjet for listing
    private readonly IConfiguration _config;
    private readonly ILogger<AuctionController> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _listingServiceBase;

    public AuctionController(ILogger<AuctionController> logger, IConfiguration config, HttpClient httpClient, IAuctionMongoDBService auctionService)
    {
        _auctionService = auctionService;
        
        //tilføjet for listing
        _config = config;
        _logger = logger;
        _httpClient = httpClient;
        _listingServiceBase = _config["LISTINGSERVICE_ENDPOINT"] ?? "http://localhost:5077";
    }

    [HttpPost("generate-from-listings")]
    public async Task<IActionResult> GenerateFromListings()
    {
        try
        {
            var auctions = await _auctionService.CreateAuctionsFromListingsAsync();
            return Ok(new
            {
                message = $"{auctions.Count} auctions created.",
                auctions
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAllAuctions([FromQuery] AuctionStatus? status)
    {
        try
        {
            var auctions = await _auctionService.GetAuctionsByStatusAsync(status);
            return Ok(auctions);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    
    [HttpPost("{auctionId}/bid")]
    public async Task<IActionResult> PlaceBid(string auctionId, [FromBody] BidRequest request)
    {
        try
        {
            var updatedAuction = await _auctionService.PlaceBidAsync(auctionId, request);
            return Ok(updatedAuction);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
    
    [HttpPost("{auctionId}/open")]
    public async Task<IActionResult> Open(string auctionId)
    {
        try
        {
            var opened = await _auctionService.OpenAuctionAsync(auctionId);
            return Ok(opened);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{auctionId}/close")]
    public async Task<IActionResult> Close(string auctionId)
    {
        try
        {
            var closed = await _auctionService.CloseAuctionAsync(auctionId);
            return Ok(closed);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{auctionId}/winner")]
    public async Task<IActionResult> GetWinner(string auctionId)
    {
        try
        {
            var winner = await _auctionService.GetAuctionWinnerAsync(auctionId);
            return Ok(winner);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
    [HttpPut("{auctionId}/pickup")]
    public async Task<IActionResult> PickUp(string auctionId)
    {
        try
        {
            var updated = await _auctionService.UpdatePickUpAsync(auctionId);
            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            // Auction not found
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            // Any other failure
            return BadRequest(new { error = ex.Message });
        }
    }
    
    [HttpGet("ping")]
    public ActionResult<bool> Ping()
    {
        return Ok(true);
    }
    
    
    //Listings
    // [HttpGet("listings")]
    // public async Task<IActionResult> GetAllListings()
    // {
    //     try
    //     {
    //         // hvis status ikke er angivet, retunerer den alle
    //         var listings = await _auctionService.ReturnAllListings();
    //         return Ok(listings);
    //     }
    //     catch (Exception ex)
    //     {
    //         return BadRequest(new { error = ex.Message });
    //     }
    // }
    //
    // [HttpGet("external-listings")]
    // public async Task<IActionResult> GetExternalListings()
    // {
    //     var validateUrl = $"{_listingServiceBase}/api/listing";
    //
    //     _logger.LogInformation("Kalder ListingService på {Url}", validateUrl);
    //     var response = await _httpClient.GetAsync(validateUrl);
    //     if (response.IsSuccessStatusCode)
    //     {
    //         var listings = await response.Content.ReadFromJsonAsync<List<Listing>>();
    //         return Ok(listings);
    //     }
    //
    //     return BadRequest(new { error = "Failed to fetch listings from external service." });
    // }
    
    [Authorize]
    [HttpGet("authcheck")]
    public async Task<IActionResult> Get()
    {
        return Ok("You're authorized");
    }
    
    
}