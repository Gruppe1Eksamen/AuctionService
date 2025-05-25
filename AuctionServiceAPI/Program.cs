using AuctionService.Models;
using AuctionService.Services;
using MongoDB.Driver;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using VaultSharp;
using VaultSharp.V1.AuthMethods.Token;
using VaultSharp.V1.AuthMethods;
using VaultSharp.V1.Commons;
using NLog;
using NLog.Web;

// Setup NLog
var logger = LogManager
    .Setup()
    .LoadConfigurationFromFile("NLog.config")
    .GetCurrentClassLogger();

logger.Debug("Init main");

// Vault setup before app builder
var endPoint = Environment.GetEnvironmentVariable("VAULT_ENDPOINT") ?? "https://localhost:8201";
logger.Info($"VAULT_ENDPOINT: {endPoint}");

var httpClientHandler = new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
};

IAuthMethodInfo authMethod = new TokenAuthMethodInfo("00000000-0000-0000-0000-000000000000");
var vaultClientSettings = new VaultClientSettings(endPoint, authMethod)
{
    MyHttpClientProviderFunc = handler => new HttpClient(httpClientHandler)
    {
        BaseAddress = new Uri(endPoint)
    }
};
IVaultClient vaultClient = new VaultClient(vaultClientSettings);

Secret<SecretData> kv2Secret;
try
{
    kv2Secret = await ReadVaultSecretWithRetryAsync(
        vaultClient,
        path: "passwords",
        mountPoint: "secret",
        maxRetries: 5,
        delayBetweenRetries: TimeSpan.FromSeconds(5));
}
catch (Exception ex)
{
    logger.Error("Failed to fetch secrets from Vault after 5 attempts: " + ex.Message);
    return;
}

var mySecret = kv2Secret.Data.Data["Secret"].ToString();
var myIssuer = kv2Secret.Data.Data["Issuer"].ToString();
logger.Info($"Vault Issuer: {myIssuer}");

// Build the app
var builder = WebApplication.CreateBuilder(args);

// Inject Vault secrets into config
builder.Configuration["Secret"] = mySecret;
builder.Configuration["Issuer"] = myIssuer;

// Enable NLog for ASP.NET Core
builder.Logging.ClearProviders();
builder.Logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
builder.Host.UseNLog();

// Authentication
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = myIssuer,
            ValidAudience = "http://localhost",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(mySecret))
        };
    });

// MongoDB config
var mongoConn = builder.Configuration["MongoConnectionString"] ??
                throw new InvalidOperationException("Missing MongoConnectionString");
var databaseName = builder.Configuration["AuctionDatabase"] ??
                   throw new InvalidOperationException("Missing AuctionDatabase");
var collectionName = builder.Configuration["AuctionCollection"] ??
                     throw new InvalidOperationException("Missing AuctionCollection");

builder.Services.AddSingleton<IMongoDBContext, MongoDBContext>();
builder.Services.AddSingleton<IAuctionMongoDBService, AuctionMongoDBService>();

// ListingService HTTP client
var listingEndpoint = builder.Configuration["LISTINGSERVICE_ENDPOINT"]
                      ?? throw new InvalidOperationException("Missing LISTINGSERVICE_ENDPOINT");

builder.Services.AddHttpClient<IListingClient, ListingClient>(client =>
{
    client.BaseAddress = new Uri(listingEndpoint);
});

// Controllers and Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

// app.UseHttpsRedirection(); // Enable if needed
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Vault retry helper
static async Task<Secret<SecretData>> ReadVaultSecretWithRetryAsync(
    IVaultClient vaultClient,
    string path,
    string mountPoint,
    int maxRetries = 5,
    TimeSpan? delayBetweenRetries = null)
{
    delayBetweenRetries ??= TimeSpan.FromSeconds(5);

    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            return await vaultClient.V1.Secrets.KeyValue.V2
                .ReadSecretAsync(path: path, mountPoint: mountPoint);
        }
        catch (Exception ex) when (attempt < maxRetries)
        {
            Console.WriteLine(
                $"[Vault] Attempt {attempt} failed: {ex.Message}. Retrying in {delayBetweenRetries.Value.TotalSeconds}s...");
            await Task.Delay(delayBetweenRetries.Value);
        }
    }

    throw new Exception($"Failed to read secret from Vault after {maxRetries} attempts.");
}
