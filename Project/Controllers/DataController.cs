using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Threading.Tasks;

namespace GetIpAddressApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class IpController : ControllerBase
    {
        private static readonly HttpClient client = new HttpClient();

        [HttpGet]
        [Route("get-public-ip")]
        public async Task<IActionResult> GetPublicIpAddress()
        {
            try
            {
                string publicIpAddress = await GetPublicIPAddressAsync();
                return Ok(new { PublicIpAddress = publicIpAddress });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Failed to retrieve public IP address", Error = ex.Message });
            }
        }

        private async Task<string> GetPublicIPAddressAsync()
        {
            // Using an external service to fetch public IP
            HttpResponseMessage response = await client.GetAsync("https://api.ipify.org");
            response.EnsureSuccessStatusCode();

            // The public IP address will be in the response content
            string publicIpAddress = await response.Content.ReadAsStringAsync();
            return publicIpAddress;
        }
    }
}
