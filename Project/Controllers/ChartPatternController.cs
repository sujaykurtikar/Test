using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class ChartPatternController : ControllerBase
{
    private  ChartPatternDetector _detector;
    private readonly HistoricalDataFetcher _fetcher;
    private TimeZoneInfo istTimeZone;
    public ChartPatternController(HistoricalDataFetcher fetcher)
    {
        _fetcher = fetcher;
        _detector = null; // Detector will be initialized after fetching data
        istTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"); 
    }

    // Common method to fetch and prepare historical candlestick data
    private async Task<List<Candlestick>> FetchHistoricalCandlestickData(string resolution = "15m")
    {
        var endDate = DateTime.UtcNow;
        var fetcher = new HistoricalDataFetcher();

        int value = int.Parse(new string(resolution.Where(char.IsDigit).ToArray()));
        var startDate = DateTime.UtcNow.AddMinutes(-(1000 * value)); // max 2000

        List<Candlestick> historicalData = await fetcher.FetchCandles("BTCUSD", resolution, startDate, endDate);

        historicalData.Reverse();

        return historicalData;
    }

    [HttpGet("detect-descending-triangle")]
    public async Task<IActionResult> DetectDescendingTriangle(string resolution = "15m")
    {
        var historicalData = await FetchHistoricalCandlestickData(resolution);
        _detector = new ChartPatternDetector(historicalData);

        var result = _detector.DetectLastDescendingTriangle();
        if (result.HasValue)
        {
            var convertedTime = TimeZoneInfo.ConvertTime(DateTimeOffset.FromUnixTimeSeconds(result.Value.breakoutTime ?? 0).UtcDateTime, istTimeZone );
            return Ok(new { convertedTime, result.Value.side });
        }
        return NotFound("No Descending Triangle breakout detected.");
    }

    [HttpGet("detect-ascending-triangle")]
    public async Task<IActionResult> DetectAscendingTriangle(string resolution = "15m")
    {
        var historicalData = await FetchHistoricalCandlestickData(resolution);
        _detector = new ChartPatternDetector(historicalData);

        var result = _detector.DetectLastAscendingTriangle();
        if (result.HasValue)
        {
            var convertedTime = TimeZoneInfo.ConvertTime(DateTimeOffset.FromUnixTimeSeconds(result.Value.breakoutTime ?? 0).UtcDateTime, istTimeZone);
            return Ok(new { convertedTime, result.Value.side });
        }
        return NotFound("No Ascending Triangle breakout detected.");
    }

    [HttpGet("detect-double-top")]
    public async Task<IActionResult> DetectDoubleTop(string resolution = "15m")
    {
        var historicalData = await FetchHistoricalCandlestickData(resolution);
        _detector = new ChartPatternDetector(historicalData);

        var result = _detector.DetectLastDoubleTop();
        if (result.HasValue)
        {
            var convertedTime = TimeZoneInfo.ConvertTime(DateTimeOffset.FromUnixTimeSeconds(result.Value.breakoutTime ?? 0).UtcDateTime, istTimeZone);
            return Ok(new { convertedTime, result.Value.side });
        }
        return NotFound("No Double Top breakout detected.");
    }

    [HttpGet("detect-double-bottom")]
    public async Task<IActionResult> DetectDoubleBottom(string resolution = "15m")
    {
        var historicalData = await FetchHistoricalCandlestickData(resolution);
        _detector = new ChartPatternDetector(historicalData);

        var result = _detector.DetectLastDoubleBottom();
        if (result.HasValue)
        {
            var convertedTime = TimeZoneInfo.ConvertTime(DateTimeOffset.FromUnixTimeSeconds(result.Value.breakoutTime ?? 0).UtcDateTime, istTimeZone);
            return Ok(new { convertedTime, result.Value.side });
        }
        return NotFound("No Double Bottom breakout detected.");
    }
}
