using System.Text.Json;
using AuctionService.Models;

namespace AuctionService.Services;

public class ListingClient : IListingClient
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ListingClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<Listing>> GetAllListingsAsync()
    {
        var response = await _httpClient.GetAsync("/api/listings/");
        if (!response.IsSuccessStatusCode)
            return new List<Listing>();

        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer
                   .Deserialize<List<Listing>>(content, _jsonOptions)
               ?? new List<Listing>();
    }
}
//man fetcher alle listings
//hvis en listing allerede eksisterer inde i en auktion, skal den ikke oprettes