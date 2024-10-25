using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Threading.Tasks;

[ApiController]
[Route("[controller]")]
public class TickerController : ControllerBase
{
    private readonly DeltaAPI _deltaApi;
    private readonly IConfiguration _configuration;
    public TickerController(IConfiguration configuration)
    {
        _configuration = configuration;
        // Initialize DeltaAPI with your API key and secret
        _deltaApi = new DeltaAPI("your-api-key", "your-api-secret", _configuration);
    }

    [HttpGet("{symbol}")]
    public async Task<IActionResult> GetTicker(string symbol)
    {
        try
        {
            // Call the DeltaAPI to get ticker information
            JObject ticker = await _deltaApi.GetTickerAsync(symbol);

            string jsonString = ticker.ToString();

            // Return ContentResult with appropriate headers
            return new ContentResult
            {
                Content = jsonString,
                ContentType = "application/json; charset=utf-8",
                StatusCode = (int)HttpStatusCode.OK
            };
            // Return the ticker information as JSON
            //return ticker;
        }
        catch (HttpRequestException e)
        {
            // Handle request errors
          //  return StatusCode(500, $"Request error: {e.Message}");
        }
        catch (Exception e)
        {
            // Handle general errors
         //   return StatusCode(500, $"General error: {e.Message}");
        }
        return null;
    }
}
