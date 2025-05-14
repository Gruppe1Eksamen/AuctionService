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

}