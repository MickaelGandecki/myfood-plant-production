using RestSharp;
using RestSharp.Authenticators;
using Newtonsoft.Json;
using PlantApp.Models;

namespace PlantApp.Services;

public class OdooService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<OdooService> _logger;
    private string? _accessToken;
    private DateTime _tokenExpiry;

    public OdooService(IConfiguration configuration, ILogger<OdooService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    private async Task<string> GetAccessTokenAsync()
    {
        if (!string.IsNullOrEmpty(_accessToken) && DateTime.Now < _tokenExpiry)
        {
            _logger.LogDebug("Using cached access token, expires at {ExpiryTime}", _tokenExpiry);
            return _accessToken;
        }

        var tokenUrl = _configuration["Odoo:TokenUrl"] ?? "https://localhost";
        var clientId = _configuration["Odoo:ClientId"];
        var clientSecret = _configuration["Odoo:ClientSecret"];
        
        _logger.LogInformation("Requesting new access token from {TokenUrl}", tokenUrl);
        _logger.LogDebug("Using client_id: {ClientId}", clientId);
        
        var client = new RestClient(tokenUrl);
        var request = new RestRequest("", Method.Post);
        
        request.AddParameter("grant_type", "client_credentials");
        request.AddParameter("client_id", clientId);
        request.AddParameter("client_secret", clientSecret);

        var response = await client.ExecuteAsync(request);
        
        _logger.LogDebug("Token request response status: {StatusCode}", response.StatusCode);
        _logger.LogDebug("Token request response content: {Content}", response.Content?.Substring(0, Math.Min(response.Content?.Length ?? 0, 200)));
        
        if (!response.IsSuccessful)
        {
            _logger.LogError("Failed to get access token. Status: {StatusCode}, Error: {ErrorMessage}, Content: {Content}", 
                response.StatusCode, response.ErrorMessage, response.Content);
            throw new Exception($"Failed to get access token: {response.ErrorMessage} - Status: {response.StatusCode}");
        }

        dynamic? tokenResponse = JsonConvert.DeserializeObject(response.Content!);
        _accessToken = tokenResponse?.access_token;
        _tokenExpiry = DateTime.Now.AddSeconds((double)(tokenResponse?.expires_in ?? 3600));
        
        _logger.LogInformation("Successfully obtained access token, expires at {ExpiryTime}", _tokenExpiry);

        return _accessToken!;
    }

    public async Task<List<ManufacturingOrder>> GetManufacturingOrdersAsync(string state = "confirmed")
    {
        _logger.LogInformation("Getting manufacturing orders with state: {State}", state);
        
        try
        {
            var token = await GetAccessTokenAsync();
            var apiUrl = _configuration["Odoo:ApiUrl"] ?? "https://localhost";
            var endpoint = "/custom/get-manufacturing-orders-for-plants";
            
            _logger.LogDebug("API URL: {ApiUrl}, Endpoint: {Endpoint}", apiUrl, endpoint);
            
            var client = new RestClient(apiUrl);
            var request = new RestRequest(endpoint, Method.Get);
            request.AddHeader("Authorization", $"Bearer {token}");
            request.AddParameter("state", state);
            
            _logger.LogDebug("Sending request to get manufacturing orders");
            var response = await client.ExecuteAsync(request);
            
            _logger.LogDebug("Response status: {StatusCode}", response.StatusCode);
            _logger.LogDebug("Response content: {Content}", response.Content?.Substring(0, Math.Min(response.Content?.Length ?? 0, 500)));
            
            if (!response.IsSuccessful)
            {
                _logger.LogError("Failed to get manufacturing orders. Status: {StatusCode}, Error: {ErrorMessage}, Content: {Content}", 
                    response.StatusCode, response.ErrorMessage, response.Content);
                throw new Exception($"Failed to get manufacturing orders: {response.ErrorMessage} - Status: {response.StatusCode}");
            }

            var apiResponse = JsonConvert.DeserializeObject<ApiResponse<List<ManufacturingOrder>>>(response.Content!);
            var orders = apiResponse?.Result?.Data ?? new List<ManufacturingOrder>();
            
            _logger.LogInformation("Successfully retrieved {Count} manufacturing orders", orders.Count);
            
            if (orders.Count == 0)
            {
                _logger.LogWarning("No manufacturing orders found for state: {State}", state);
            }
            else
            {
                foreach (var order in orders.Take(5)) // Log first 5 orders for debugging
                {
                    _logger.LogDebug("Order: {OrderKey}, Product: {ProductId}, Qty: {Qty}, State: {State}", 
                        order.OrderKey, order.ProductId, order.ProductQty, order.State);
                }
            }
            
            return orders;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting manufacturing orders");
            throw;
        }
    }

    public async Task<ManufacturingOrder?> GetManufacturingOrderAsync(int orderId)
    {
        var token = await GetAccessTokenAsync();
        var client = new RestClient(_configuration["Odoo:ApiUrl"] ?? "https://localhost");
        
        var request = new RestRequest($"/api/manufacturing-orders/{orderId}", Method.Get);
        request.AddHeader("Authorization", $"Bearer {token}");

        var response = await client.ExecuteAsync(request);
        if (!response.IsSuccessful)
            return null;

        return JsonConvert.DeserializeObject<ManufacturingOrder>(response.Content!);
    }

    public async Task<bool> CompleteManufacturingOrderAsync(int orderId, List<ScannedPlant> scannedPlants)
    {
        var token = await GetAccessTokenAsync();
        var client = new RestClient(_configuration["Odoo:ApiUrl"] ?? "https://localhost");
        
        var request = new RestRequest($"/api/manufacturing-orders/{orderId}/complete", Method.Post);
        request.AddHeader("Authorization", $"Bearer {token}");
        request.AddJsonBody(new
        {
            orderId,
            scannedPlants = scannedPlants.Select(p => new
            {
                plantId = p.PlantId,
                sowingDate = p.SowingDate,
                scannedAt = p.ScannedAt
            })
        });

        var response = await client.ExecuteAsync(request);
        return response.IsSuccessful;
    }
}