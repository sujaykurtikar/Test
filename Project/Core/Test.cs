//using Microsoft.AspNetCore.Mvc;
//using Newtonsoft.Json;
//using System;
//using System.Collections.Generic;
//using System.Net.Http;
//using System.Net.Http.Headers;
//using System.Threading.Tasks;

//[ApiController]
//[Route("[controller]")]
//public class TradingController : ControllerBase
//{
//    [HttpGet(Name = "GetTest")]
//    public async Task<IActionResult> GetTest()
//    {
//        var fetcher = new HistoricalDataFetcher();
//        var resolution = "5m";
//        var startDate = DateTime.UtcNow.AddMinutes(-2000 * 5); // 2000 minutes ago
//        var endDate = DateTime.UtcNow;
//        var symbol = "BTCUSD";

//        List<Candlestick> historicalData = await fetcher.FetchCandles(symbol, resolution, startDate, endDate);

//        if (historicalData == null)
//        {
//            return StatusCode(500, "Failed to retrieve historical data.");
//        }

//        HaramiTradingStrategy strategy = new HaramiTradingStrategy();
//        strategy.RunStrategy(historicalData);

//        var supportResistanceLevels = SupportResistanceIdentifier.IdentifyLevels(historicalData);

//        return Ok(supportResistanceLevels);


//        //  return Ok("Strategy run completed.");
//    }
//}

//public class HaramiTradingStrategy
//{
//    public void RunStrategy(List<Candlestick> historicalData)
//    {
//        for (int i = 1; i < historicalData.Count; i++)
//        {
//            Candlestick current = historicalData[i];
//            Candlestick previous = historicalData[i - 1];

//            // Check for bullish Harami
//            if (IsBullishHarami(previous, current))
//            {
//                Console.WriteLine($"Bullish Harami identified on {current.Time}: Buy signal");
//                // Implement buy logic or alert here
//            }
//            // Check for bearish Harami
//            else if (IsBearishHarami(previous, current))
//            {
//                Console.WriteLine($"Bearish Harami identified on {current.Time}: Sell signal");
//                // Implement sell or short logic here
//            }
//        }
//    }

//    private bool IsBullishHarami(Candlestick previous, Candlestick current)
//    {

//        return previous.Close < previous.Open &&
//               current.Close > current.Open &&
//               current.Open > previous.Close &&
//               current.Close < previous.Open;
//    }

//    private bool IsBearishHarami(Candlestick previous, Candlestick current)
//    {
//        // Bearish Harami: Previous candle is bullish (green) and current candle is bearish (red)
//        return previous.Close > previous.Open && // Previous candle is green
//               current.Close < current.Open &&   // Current candle is red
//               current.Open < previous.Close &&  // Current candle opens within previous candle's body
//               current.Close > previous.Open;    // Current candle closes within previous candle's body
//    }
//}

//public class HistoricalDataFetcher
//{
//    public async Task<List<Candlestick>> FetchCandles(string symbol, string resolution, DateTime start, DateTime end)
//    {
//        using (var client = new HttpClient())
//        {
//            try
//            {
//                client.DefaultRequestHeaders.Accept.Clear();
//                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
//                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.3");

//                var startTimestamp = new DateTimeOffset(start).ToUnixTimeSeconds();
//                var endTimestamp = new DateTimeOffset(end).ToUnixTimeSeconds();

//                var url = $"https://cdn.india.deltaex.org/v2/history/candles?resolution={resolution}&symbol={symbol}&start={startTimestamp}&end={endTimestamp}";

//                using (var response = await client.GetAsync(url))
//                {
//                    if (response.IsSuccessStatusCode)
//                    {
//                        var json = await response.Content.ReadAsStringAsync();
//                        var data = JsonConvert.DeserializeObject<HistoricalData>(json);
//                        return ConvertToCandles(data.Result);
//                    }
//                    else
//                    {
//                        Console.WriteLine($"Failed to retrieve data. Status code: {response.StatusCode}");
//                        var errorContent = await response.Content.ReadAsStringAsync();
//                        Console.WriteLine($"Error content: {errorContent}");
//                        return null;
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"An error occurred: {ex.Message}");
//                return null;
//            }
//        }
//    }

//    private List<Candlestick> ConvertToCandles(List<CandlestickData> rawData)
//    {
//        var candles = new List<Candlestick>();
//        foreach (var item in rawData)
//        {
//            candles.Add(new Candlestick
//            {
//                Time = item.Time,
//                Open = item.Open,
//                High = item.High,
//                Low = item.Low,
//                Close = item.Close,
//                Volume = item.Volume
//            });
//        }
//        return candles;
//    }
//}

//public class Candlestick
//{
//    public long Time { get; set; }
//    public decimal Open { get; set; }
//    public decimal High { get; set; }
//    public decimal Low { get; set; }
//    public decimal Close { get; set; }
//    public decimal Volume { get; set; }
//}

//public class HistoricalData
//{
//    public List<CandlestickData> Result { get; set; }
//}

//public class CandlestickData
//{
//    public long Time { get; set; }
//    public decimal Open { get; set; }
//    public decimal High { get; set; }
//    public decimal Low { get; set; }
//    public decimal Close { get; set; }
//    public decimal Volume { get; set; }
//}

//public static class SupportResistanceIdentifier
//{
//    public static List<Level> IdentifyLevels(List<Candlestick> historicalData, int period = 5, int numberOfLevels = 3)
//    {
//        var levels = new List<Level>();

//        for (int i = period; i < historicalData.Count - period; i++)
//        {
//            var isSupport = true;
//            var isResistance = true;

//            for (int j = 1; j <= period; j++)
//            {
//                if (historicalData[i].Low > historicalData[i - j].Low || historicalData[i].Low > historicalData[i + j].Low)
//                {
//                    isSupport = false;
//                }
//                if (historicalData[i].High < historicalData[i - j].High || historicalData[i].High < historicalData[i + j].High)
//                {
//                    isResistance = false;
//                }
//            }

//            if (isSupport)
//            {
//                levels.Add(new Level { Type = LevelType.Support, Price = historicalData[i].Low, Time = historicalData[i].Time });
//            }
//            if (isResistance)
//            {
//                levels.Add(new Level { Type = LevelType.Resistance, Price = historicalData[i].High, Time = historicalData[i].Time });
//            }
//        }

//        // Get the latest support and resistance levels
//        var latestSupports = levels.Where(l => l.Type == LevelType.Support).OrderByDescending(l => l.Time).Take(numberOfLevels).ToList();
//        var latestResistances = levels.Where(l => l.Type == LevelType.Resistance).OrderByDescending(l => l.Time).Take(numberOfLevels).ToList();

//        return latestSupports.Concat(latestResistances).OrderBy(l => l.Time).ToList();
//    }
//}

//public class Level
//{
//    public LevelType Type { get; set; }
//    public decimal Price { get; set; }
//    public long Time { get; set; }
//}

//public enum LevelType
//{
//    Support,
//    Resistance
//}