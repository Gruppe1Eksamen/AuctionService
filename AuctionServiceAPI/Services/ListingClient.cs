using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using AuctionService.Models;

namespace AuctionService.Services
{
    public class ListingClient : IListingClient
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions 
            = new() { PropertyNameCaseInsensitive = true };

        public ListingClient(HttpClient httpClient)
        {
            _httpClient = httpClient 
                          ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task<List<Listing>> GetAllListingsAsync()
        {
            // Vi bruger den BaseAddress, som du har sat i Program.cs via AddHttpClient
            var response = await _httpClient.GetAsync("/api/listings/");
            if (!response.IsSuccessStatusCode)
            {
                // Log evt. her, hvis du har en ILogger til rådighed
                return new List<Listing>();
            }

            // Deserialiser direkte fra stream for bedre performance
            await using var stream = await response.Content.ReadAsStreamAsync();
            var listings = await JsonSerializer
                               .DeserializeAsync<List<Listing>>(stream, _jsonOptions)
                           ?? new List<Listing>();

            return listings;
        }
    }
}