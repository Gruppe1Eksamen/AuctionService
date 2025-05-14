namespace DefaultNamespace;

public class MongoDBContext
{
    public IMongoDatabase Database { get; set; }
    public IMongoCollection<Auction> Collection { get; set; }
    
    public MongoDBContext(ILogger<AuctionMongoDBService> logger, IConfiguration config)
    {        
        BsonSerializer.RegisterSerializer(new GuidSerializer(BsonType.String));

        var client = new MongoClient(config["MongoConnectionString"]);
        Database = client.GetDatabase(config["AuctionDB"]);
        Collection = Database.GetCollection<Listing>(config["Auctions"]);

        logger.LogInformation($"Connected to database {config["AuctionDB"]}");
        logger.LogInformation($"Using collection {config["Auctions"]}");
    }
}