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

builder.Services.AddSingleton<MongoDBContext>();

builder.Services.AddSingleton<IAuctionMongoDBService, AuctionMongoDBService>();
builder.Services.AddSingleton<ICatalogClient, CatalogClient>();

builder.Services.AddHttpClient<ICatalogClient, CatalogClient>(client =>
{
    client.BaseAddress = new Uri("http://catalog-service"); //til docker
});


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();


    app.UseSwagger();
    app.UseSwaggerUI();


app.UseHttpsRedirection();


app.Run();


