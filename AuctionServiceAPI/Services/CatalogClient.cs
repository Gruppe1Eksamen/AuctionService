using System.Text.Json;
using AuctionService.Models;

namespace AuctionService.Services;

public class CatalogClient : ICatalogClient
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public CatalogClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<Catalog?> GetCatalogAsync(Guid catalogId)
    {
        var response = await _httpClient.GetAsync($"/api/catalogs/{catalogId}");
        if (!response.IsSuccessStatusCode)
            return null;

        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<Catalog>(content, _jsonOptions);
    }
}