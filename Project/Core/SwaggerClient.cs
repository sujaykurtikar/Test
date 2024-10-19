using Serilog;
using System;
using System.Net.Http;
using System.Threading.Tasks;

public class SwaggerClient
{
    private readonly HttpClient _httpClient;

    public SwaggerClient()
    {
        _httpClient = new HttpClient();
    }

    public async Task<string> GetSwaggerPageAsync()
    {
        var url = "http://testtrading.somee.com/publish/swagger/index.html";

        try
        {
            // Make the GET request
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            // Read the HTML content of the Swagger page
            var content = await response.Content.ReadAsStringAsync();
            return content;
        }
        catch (HttpRequestException e)
        {
            Log.Information($"GetSwaggerPageAsync Request error : {e.Message}");
            return null;
        }
    }
}


