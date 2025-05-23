using AuctionService.Models;
using AuctionService.Services;
using MongoDB.Driver;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

var mongoConn = builder.Configuration["MongoConnectionString"] ??
                throw new InvalidOperationException("Missing ConnectionString");
var databaseName = builder.Configuration["AuctionDatabase"] ??
                   throw new InvalidOperationException("Missing AuctionDatabase");
var collectionName = builder.Configuration["AuctionCollection"] ??
                     throw new InvalidOperationException("Missing AuctionCollection");

builder.Services.AddSingleton<IMongoDBContext, MongoDBContext>();

builder.Services.AddSingleton<IAuctionMongoDBService, AuctionMongoDBService>();

var listingEndpoint = builder.Configuration["LISTINGSERVICE_ENDPOINT"]
                      ?? throw new InvalidOperationException("Missing LISTINGSERVICE_ENDPOINT");

builder.Services.AddHttpClient<IListingClient, ListingClient>(client =>
{
    client.BaseAddress = new Uri(listingEndpoint);
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();


    app.UseSwagger();
    app.UseSwaggerUI();


app.UseHttpsRedirection();

app.MapControllers();
app.Run();


