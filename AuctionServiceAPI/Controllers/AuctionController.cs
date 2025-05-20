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

    [HttpPost("generate-from-catalog/{catalogId}")]
    public async Task<IActionResult> GenerateFromCatalog(Guid catalogId)
    {
        try
        {
            var auctions = await _auctionService.CreateAuctionsFromCatalog(catalogId);
            return Ok(new { message = $"{auctions.Count} auctions created.", auctions });
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
        var opened = await _auctionService.OpenAuctionAsync(auctionId);
        return Ok(opened);
    }

    
    [HttpPost("{auctionId}/close")]
    public async Task<IActionResult> Close(Guid auctionId)
    {
        var closed = await _auctionService.CloseAuctionAsync(auctionId);
        return Ok(closed);
    }
    
    [HttpGet("{auctionId}/winner")]
    public async Task<IActionResult> GetWinner(Guid auctionId)
    {
        var winner = await _auctionService.GetAuctionWinnerAsync(auctionId);
        return Ok(winner);
    }


}