using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using System.Security.Cryptography;
using Serilog;
using System.Runtime.InteropServices;

public class DeltaAPI
{
    private static readonly string BaseUrl = "https://cdn.india.deltaex.org"; //"https://api.delta.exchange";
    //private static readonly string _apiKey = "VaAxvKNs64yoJneE9XBJcWCnxEgVfn";
   // private static readonly string  _apiSecret = "KVh7hGEfjo5P2db2SDBG6KSkxBea2A5DLI9CoRD6axheAEZf8eh4AD5hYezT";
    private static readonly string _apiKey = "39UYXJu80u1qqeE1gsaJo7j5JoGUb0";
    private static readonly string _apiSecret = "M0cbKEVqeeGzghl2MORxyYO6hTyzHrAhCLgevp7XXt26WOzCNEBAhYtqHmep";
    private readonly HttpClient _client;

    public DeltaAPI(string apiKey, string apiSecret)
    {
        ///_apiKey = apiKey;
        //_apiSecret = apiSecret;
        _client = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        _client.DefaultRequestHeaders.Add("api-key", _apiKey);
        _client.DefaultRequestHeaders.Add("api-secret", _apiSecret);
    }

    // Fetch ticker information
    public async Task<JObject> GetTickerAsync(string symbol)
    {
        //var response = await _client.GetAsync($"/v2/tickers/{symbol}");
        //response.EnsureSuccessStatusCode();
        //var content = await response.Content.ReadAsStringAsync();
        //return JsonConvert.DeserializeObject<JObject>(content);

        var url = $"https://cdn.india.deltaex.org/v2/tickers/{symbol}";

        using (var httpClient = new HttpClient())
        {
            try
            {
                // Adding headers
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.36");

                var response = await httpClient.GetStringAsync(url);
             //   var tickerInfo = JObject.Parse(response);
                var tickerInfo = JObject.Parse(response)["result"];

                // Extract and convert values
                
               // Console.WriteLine(tickerInfo.ToString());
                return (JObject)tickerInfo;
            }
            catch (HttpRequestException e)
            {
                Log.Information($"Request error: {e.Message}");
            }
            catch (Exception e)
            {
                Log.Information($"General error: {e.Message}");
            }
        }
        return null;
    }

    // Place a Limit Order
    public async Task<JObject> PlaceOrderAsync(int productId, decimal qty, string sides, decimal limitPrice)
    {
        //var orderPayload = new
        //{
        //    product_id = productId,
        //    size = qty,
        //    side = side,
        //    limit_price = limitPrice
        //};

       // var content = new StringContent(JsonConvert.SerializeObject(orderPayload));
        //content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        //var response = await _client.PostAsync("/v2/orders", content);
        //response.EnsureSuccessStatusCode();
        //var responseContent = await response.Content.ReadAsStringAsync();
        //return JsonConvert.DeserializeObject<JObject>(responseContent);

        using (var httpClient = new HttpClient())
        {
            try
            {
                // Adding headers
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.36");

                // Prepare content for POST request
                //var orderPayload = new
                //{
                //    product_id = productId,
                //    size = qty,
                //    side = side,
                //    limit_price = limitPrice
                //};
                //var content = new StringContent(JsonConvert.SerializeObject(orderPayload));
                //content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                //// Send POST request
                //var response = await httpClient.PostAsync("https://cdn.india.deltaex.org/v2/orders", content);
                //response.EnsureSuccessStatusCode();
                //var responseContent = await response.Content.ReadAsStringAsync();
                //return JsonConvert.DeserializeObject<JObject>(responseContent);



                // Prepare the order data
                //var orderDatad = new
                //{
                //    product_id = 27, // Product ID for BTCUSD is 27
                //    size = 1,
                //    order_type = "market_order",
                //    side = "buy"
                //};
                var trailAmount = 40;
                var stop_loss_limit_price = limitPrice - 70; // Replace "string" with a value
                var stop_loss_price = limitPrice - 40; // Replace "string" with a value
                var take_profit_limit_price = limitPrice + 250; // Replace "string" with a value
                var take_profit_price = limitPrice + 200; // Replace "string" with a value

                if (sides == "sell")
                {
                     //stop_loss_limit_price = limitPrice - 40; // Replace "string" with a value
                     //stop_loss_price = limitPrice - 70; // Replace "string" with a value
                     //take_profit_limit_price = limitPrice + 100; // Replace "string" with a value
                     //take_profit_price = limitPrice + 150; // Replace "string" with a value

                    stop_loss_limit_price = limitPrice + 70; // Replace "string" with a value
                    stop_loss_price = limitPrice + 40; // Replace "string" with a value
                    take_profit_limit_price = limitPrice - 250; // Replace "string" with a value
                    take_profit_price = limitPrice - 200; // Replace "string" with a value
                    trailAmount = -40;
                }

                var orderData = new
                {
                    product_id = productId,
                    size = qty,
                    side = sides,
                    limit_price = limitPrice,
                    order_type = "limit_order",//"market_order",// "limit_order",
                    stop_order_type = "stop_loss_order",
                    //stop_price = stop_loss_price, // Replace "string" with a dynamic value or keep it as a string if necessary
                    trail_amount = trailAmount, // Replace "string" with a dynamic value or keep it as a string if necessary
                    stop_trigger_method = "last_traded_price",//"mark_price",
                    bracket_stop_loss_limit_price = stop_loss_limit_price, // Replace "string" with a value
                    bracket_stop_loss_price = stop_loss_price, // Replace "string" with a value
                    bracket_take_profit_limit_price = take_profit_limit_price, // Replace "string" with a value
                    bracket_take_profit_price = take_profit_price, // Replace "string" with a value
                    //time_in_force = "gtc",
                    //mmp = "disabled",
                    //post_only = true, // Changed to boolean
                    //reduce_only = true, // Changed to boolean
                    //close_on_trigger = true, // Changed to boolean
                                             //  client_order_id = clientOrderId // Replace "string" with a dynamic value
                };

                string body = JsonConvert.SerializeObject(orderData, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                string method = "POST";
                string endpoint = "/v2/orders";
                (string signature, string timestamp) = GenerateSignature(method, endpoint, body);

                // Add the API key and signature to the request headers
                // var client = new HttpClient();
                //var content = new StringContent(JsonConvert.SerializeObject(orderPayload));
                //content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                //// Send POST request
                //var response = await httpClient.PostAsync("https://cdn.india.deltaex.org/v2/orders", content);

                var request = new HttpRequestMessage(HttpMethod.Post, "https://cdn.india.deltaex.org/v2/orders")
                {
                    Content = new StringContent(body, Encoding.UTF8, "application/json")
                };
                request.Headers.Add("api-key", _apiKey);
                request.Headers.Add("signature", signature);
                request.Headers.Add("timestamp", timestamp);

                try
                {
                    HttpResponseMessage response = await httpClient.SendAsync(request);
                    Log.Information("Order placed: Success={Success}, StatusCode={StatusCode}",response.IsSuccessStatusCode, response.StatusCode);
                    if (response.IsSuccessStatusCode)
                    {
                        // Read and process the response
                        string responseBody = await response.Content.ReadAsStringAsync();
                        var orderResponse = JsonConvert.DeserializeObject<JObject>(responseBody);
                        //var bracket = await PlaceBracketOrderAsync(productId,take_profit_limit_price,take_profit_price,stop_loss_limit_price,stop_loss_price);
                        //if(bracket != null) 
                        //{ 
                        ////
                        //}
                        // Log.Information("Order Response: {@OrderResponse}", orderResponse);
                        Log.Information("Order Response: {@OrderResponse}", responseBody);
                        return null;
                       // return orderResponse;
                    }
                    else
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        Log.Information("Order failed: {@ErrorContent}", error);
                    }
                    response.EnsureSuccessStatusCode();


                   // Console.WriteLine(JsonConvert.SerializeObject(orderResponse, Formatting.Indented));
                }
                catch (HttpRequestException e)
                {
                    Log.Information($"Request error: {e.Message}");
                }
            }
            catch (HttpRequestException e)
            {
                Log.Information($"Request error: {e.Message}");
            }
            catch (Exception e)
            {
                Log.Information($"General error: {e.Message}");
            }
        }
        return null;
    }

    public async Task<JObject> PlaceBracketOrderAsync(int productId, decimal t_limitPrice, decimal t_stop_price, decimal s_limitPrice, decimal s_stop_price) 
    {

        using (var httpClient = new HttpClient())
        {
            try
            {
                // Adding headers
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.36");

               var orderData = new
               {
                   product_id = productId,
                   product_symbol = "BTCUSD",
                   stop_loss_order = new
                   {
                       order_type = "limit_order",
                       stop_price = s_stop_price,
                       trail_amount = "0",
                       limit_price = s_limitPrice
                   },
                   take_profit_order = new
                   {
                       order_type = "limit_order",
                       stop_price = t_stop_price,
                       limit_price = t_limitPrice
                   },
                   stop_trigger_method = "mark_price"
               };

                string body = JsonConvert.SerializeObject(orderData, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                string method = "POST";
                string endpoint = "/v2/orders/bracket";
                (string signature, string timestamp) = GenerateSignature(method, endpoint, body);

                var request = new HttpRequestMessage(HttpMethod.Post, "https://cdn.india.deltaex.org/v2/orders/bracket")
                {
                    Content = new StringContent(body, Encoding.UTF8, "application/json")
                };
                request.Headers.Add("api-key", _apiKey);
                request.Headers.Add("signature", signature);
                request.Headers.Add("timestamp", timestamp);

                try
                {
                    HttpResponseMessage response = await httpClient.SendAsync(request);
                    if (response.IsSuccessStatusCode)
                    {
                        // Read and process the response
                        string responseBody = await response.Content.ReadAsStringAsync();
                        var orderResponse = JsonConvert.DeserializeObject<JObject>(responseBody);
                        return orderResponse;
                    }
                    else
                    {
                        Log.Information($"Response error: {await response.Content.ReadAsStringAsync()}");
                    }
                    response.EnsureSuccessStatusCode();


                    // Console.WriteLine(JsonConvert.SerializeObject(orderResponse, Formatting.Indented));
                }
                catch (HttpRequestException e)
                {
                    Log.Information($"Request error: {e.Message}");
                }
            }
            catch (HttpRequestException e)
            {
                Log.Information($"Request error: {e.Message}");
            }
            catch (Exception e)
            {
                Log.Information($"General error: {e.Message}");
            }
        }
        return null;

    }
    private static (string, string) GenerateSignature(string method, string endpoint, string payload)
    {
        string timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        string signatureData = method + timestamp + endpoint + payload;
        byte[] key = Encoding.UTF8.GetBytes(_apiSecret);
        byte[] message = Encoding.UTF8.GetBytes(signatureData);

        using (var hmac = new HMACSHA256(key))
        {
            byte[] hash = hmac.ComputeHash(message);
            string signature = BitConverter.ToString(hash).Replace("-", "").ToLower();
            return (signature, timestamp);
        }
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
