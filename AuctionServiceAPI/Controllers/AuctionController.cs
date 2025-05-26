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
    private readonly IConfiguration _config;
    private readonly ILogger<AuctionController> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _listingServiceBase;

    public AuctionController(ILogger<AuctionController> logger, IConfiguration config, HttpClient httpClient, IAuctionMongoDBService auctionService)
    {
        _auctionService = auctionService;
        
        _config = config;
        _logger = logger;
        _httpClient = httpClient;
        _listingServiceBase = _config["LISTINGSERVICE_ENDPOINT"] ?? "http://localhost:5077";
    }

    [Authorize]
    [HttpPost("generate")]
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

    [Authorize]
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

    [Authorize]
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
    
    [Authorize]
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

    [Authorize]
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

    [Authorize]
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
    
    [Authorize]
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
    [Authorize]
    [HttpGet("winner/{winnerId}")]
    public async Task<IActionResult> GetAuctionsByWinner(string winnerId)
    {
        if (string.IsNullOrWhiteSpace(winnerId))
            return BadRequest(new { error = "winnerId must be provided" });

        try
        {
            var auctions = await _auctionService.GetAuctionsByWinnerId(winnerId);
            return Ok(auctions);
        }
        catch (Exception ex)
        {
            return BadRequest();
        }
    }
    
    [AllowAnonymous]
    [HttpGet("ping")]
    public ActionResult<bool> Ping()
    {
        return Ok(true);
    }
    
    [Authorize]
    [HttpGet("authcheck")]
    public async Task<IActionResult> Get()
    {
        return Ok("You're authorized");
    }
}