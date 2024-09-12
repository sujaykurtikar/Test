using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

public class DeltaAPI
{
    private static readonly string BaseUrl = "https://api.delta.exchange";
    private readonly string _apiKey;
    private readonly string _apiSecret;
    private readonly HttpClient _client;

    public DeltaAPI(string apiKey, string apiSecret)
    {
        _apiKey = apiKey;
        _apiSecret = apiSecret;
        _client = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        _client.DefaultRequestHeaders.Add("api-key", _apiKey);
        _client.DefaultRequestHeaders.Add("api-secret", _apiSecret);
    }

    // Fetch ticker information
    public async Task<JObject> GetTickerAsync(string symbol)
    {
        var response = await _client.GetAsync($"/v2/tickers/{symbol}");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<JObject>(content);
    }

    // Place a Limit Order
    public async Task<JObject> PlaceOrderAsync(int productId, decimal qty, string side, decimal limitPrice)
    {
        var orderPayload = new
        {
            product_id = productId,
            size = qty,
            side = side,
            limit_price = limitPrice
        };

        var content = new StringContent(JsonConvert.SerializeObject(orderPayload));
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        var response = await _client.PostAsync("/v2/orders", content);
        response.EnsureSuccessStatusCode();
        var responseContent = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<JObject>(responseContent);
    }

    // Get Live Orders
    public async Task<JArray> GetLiveOrdersAsync(int productId)
    {
        var response = await _client.GetAsync($"/v2/orders/live?product_id={productId}");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<JArray>(content);
    }

    // Cancel an Order
    public async Task CancelOrderAsync(string orderId, int productId)
    {
        var cancelPayload = new { order_id = orderId, product_id = productId };
        var content = new StringContent(JsonConvert.SerializeObject(cancelPayload));
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        var response = await _client.PostAsync("/v2/orders/cancel", content);
        response.EnsureSuccessStatusCode();
        Console.WriteLine("Order canceled");
    }
}
