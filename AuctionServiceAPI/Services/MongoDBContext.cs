using AuctionService.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace AuctionService.Services;

public class MongoDBContext : IMongoDBContext
{
    public IMongoDatabase Database { get; set; }
    public IMongoCollection<Auction> Collection { get; set; }
    
    public MongoDBContext(ILogger<AuctionMongoDBService> logger, IConfiguration config)
    {        
        var client = new MongoClient(config["MongoConnectionString"]);
        Database = client.GetDatabase(config["AuctionDatabase"]);
        Collection = Database.GetCollection<Auction>(config["AuctionCollection"]);

        logger.LogInformation($"Connected to database {config["AuctionDatabase"]}");
        logger.LogInformation($"Using collection {config["AuctionCollection"]}");
    }
}