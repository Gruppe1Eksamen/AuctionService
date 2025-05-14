using AuctionService.Models
using AuctionService.Services
using MongoDB.Driver
using System.Text.Json.Serialization
    
var builder = WebApplication.CreateBuilder(args);

var mongoConn = builder.Configuration["MongoConnectionString"];


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();


    app.UseSwagger();
    app.UseSwaggerUI();


app.UseHttpsRedirection();


app.Run();


