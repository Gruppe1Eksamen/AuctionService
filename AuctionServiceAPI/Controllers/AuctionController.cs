using AuctionService.Models;
using AuctionService.Services;
using Microsoft.AspNetCore.Mvc;

namespace AuctionServiceAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuctionController : ControllerBase
{
    private readonly IAuctionMongoDBService _auctionService;

    public AuctionController(IAuctionMongoDBService auctionService)
    {
        _auctionService = auctionService;
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
            // hvis status ikke er angivet, retunerer den alle
            var auctions = await _auctionService.GetAuctionsByStatusAsync(status);
            return Ok(auctions);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    
    [HttpPost("{auctionId}/bid")]
    public async Task<IActionResult> PlaceBid(Guid auctionId, [FromBody] BidRequest request)
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
    public async Task<IActionResult> Open(Guid auctionId)
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
    public async Task<IActionResult> Close(Guid auctionId)
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
    public async Task<IActionResult> GetWinner(Guid auctionId)
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
    public async Task<IActionResult> PickUp(Guid auctionId)
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
    


}