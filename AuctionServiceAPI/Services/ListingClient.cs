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
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;
        private readonly ILogger<ListingClient> _logger;
        private readonly JsonSerializerOptions _jsonOptions 
            = new() { PropertyNameCaseInsensitive = true };
        private readonly string _listingServiceBase;


        public ListingClient(HttpClient httpClient, IConfiguration config, ILogger<ListingClient> logger)
        {
            _config = config;
            _logger = logger;
            _httpClient = httpClient 
                          ?? throw new ArgumentNullException(nameof(httpClient));
            _listingServiceBase = _config["LISTINGSERVICE_ENDPOINT"];

        }
        
        public async Task<List<Listing>> GetAllListingsAsync()
        {
            var validateUrl = $"{_listingServiceBase}/api/listing";
        
            _logger.LogInformation("Kalder ListingService på {Url}", validateUrl);
            var response = await _httpClient.GetAsync(validateUrl);
        
            var listings = await response.Content.ReadFromJsonAsync<List<Listing>>();
            return listings;
        
        }
        
        
        
    }
}