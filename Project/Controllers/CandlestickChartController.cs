using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

[ApiController]
[Route("api/[controller]")]
public class CandlestickChartController : ControllerBase
{
    private readonly ChartService _chartService;

    public CandlestickChartController(ChartService chartService)
    {
        _chartService = chartService;
    }

    [HttpPost("generate")]
    public async Task<IActionResult> GenerateCandlestickChartAsync(string resolution = "15m", int noOfCandles = 1000)
    {
        var endDate = DateTime.UtcNow;
        var fetcher = new HistoricalDataFetcher();

        int value = int.Parse(new string(resolution.Where(char.IsDigit).ToArray()));
        var startDate = DateTime.UtcNow.AddMinutes(-(noOfCandles * value)); // max 2000

        List<Candlestick> historicalData = await fetcher.FetchCandles("BTCUSD", resolution, startDate, endDate);

        historicalData.Reverse();

        var chartImage = _chartService.GenerateCandlestickChart(historicalData);

       // var chartImage = _chartService.GenerateCandlestickChart(historicalData);

        return File(chartImage, "image/png");
    }
}
