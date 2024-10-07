using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using static HaramiTradingStrategy;
using System.Globalization;
using Newtonsoft.Json;
using Microsoft.Extensions.DependencyInjection;
using static Test_Project.Controllers.ControlController;

[ApiController]
[Route("[controller]")]
public class TradingController : ControllerBase
{
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(5);
    private readonly IConfiguration _appSettings;

    public TradingController(IConfiguration configuration)
    {
        _appSettings = configuration;
    }
    [HttpGet(Name = "GetTest")]
    public async Task<IActionResult> GetTest()
    {
        var fetcher = new HistoricalDataFetcher();
        var resolution = _appSettings.GetValue<string>("Resolution");
        int value = int.Parse(new string(resolution.Where(char.IsDigit).ToArray()));
        var startDate = DateTime.UtcNow.AddMinutes(-(2000 * value)); // max 2000
        //var startDate = DateTime.UtcNow.AddMinutes(-2000 *5); // 2000 minutes ago
        var endDate = DateTime.UtcNow;
        var symbol = "BTCUSD";
        //var maxInterval = 10000 * 15;
        //var start = 2;
        //var end = 1;
        List<Candlestick> historicalData = await fetcher.FetchCandles(symbol, resolution, startDate, endDate);
       var lastcandeltime = historicalData.FirstOrDefault().Time;
        historicalData.Reverse();

        // Return the fetched historical data
        // return Ok();


        //  ExportCandlestickToCsv(historicalData, "historicalData.csv");
        // Export to JSON
        //ExportToJson(historicalData, "historicalData.json");

        //if (historicalData == null)
        //{
        //    return StatusCode(500, "Failed to retrieve historical data.");
        //}
        //else
        //{
        //    historicalData.Reverse();
        //}

        //int shortTerm = 9;
        //int longTerm = 21;

        var shortTerm = _appSettings.GetValue<int>("Period:Period1");
        var longTerm = _appSettings.GetValue<int>("Period:Period2");

        var movingAverages = MovingAverageAnalyzer.CalculateMovingAverages(historicalData, shortTerm, longTerm);
        var angles = MovingAverageAnalyzer.CalculateAngles(movingAverages, shortTerm, longTerm);

        // Call the method and handle the result
        var latestCrossover = MovingAverageAnalyzer.IdentifyCrossoversAndAngles(movingAverages, angles).LastOrDefault();


        if (latestCrossover != default)
        {
            var utcDateTime = DateTimeOffset.FromUnixTimeSeconds(latestCrossover.Timestamp).UtcDateTime;
           
            // Convert UTC DateTime to IST
            var istTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            var istDateTime = TimeZoneInfo.ConvertTime(utcDateTime, istTimeZone);

            var lastcandel = DateTimeOffset.FromUnixTimeSeconds(lastcandeltime).UtcDateTime;
            var lastcandelT = TimeZoneInfo.ConvertTime(lastcandel, istTimeZone);
            var difeer = -(DateTime.Now - TimeSpan.FromMinutes(6.20) - TimeZoneInfo.ConvertTime(lastcandel, istTimeZone));

            return Ok(new
            {
                DateTime = istDateTime.ToString("yyyy-MM-dd HH:mm:ss"),
                Type = latestCrossover.Type,
                Angle = latestCrossover.Angle,
                lastcandeltime = lastcandelT,
                difeer = difeer,
            });
        }
        else
        {
            return NoContent(); // Or any other appropriate status code
        }
    }

    [HttpGet("fetch-latest-crossover")]
    public async Task<IActionResult> FetchLatestCrossover()
    {
        try
        {
           // var startDate = DateTime.UtcNow.AddMinutes(-6000);
            
            var endDate = DateTime.UtcNow;
            var fetcher = new HistoricalDataFetcher();

            var resolution = _appSettings.GetValue<string>("Resolution");
            int value = int.Parse(new string(resolution.Where(char.IsDigit).ToArray()));
            var startDate = DateTime.UtcNow.AddMinutes(-(2000 * value)); // max 2000

            var symbol = "BTCUSD";
            
            List<Candlestick> historicalData = await fetcher.FetchCandles(symbol, resolution, startDate, endDate);
            var lastcandeltime = historicalData.FirstOrDefault().Time;
            historicalData.Reverse();
           

            int shortTerm = 7;
            int longTerm = 21;

            var emaPeriod1 = _appSettings.GetValue<int>("Period:Period1");
            var emaPeriod2 = _appSettings.GetValue<int>("Period:Period2");

            var emas = EmaAnalyzer.CalculateEmas(historicalData, emaPeriod1, emaPeriod2);
            var angles = EmaAnalyzer.CalculateEmaAngles(emas);
            var latestCrossoverEMA = EmaAnalyzer.IdentifyEmaCrossoversAndAngles(emas, angles).LastOrDefault();

            // Convert the crossover candle time to IST
            var utcDateTime = DateTimeOffset.FromUnixTimeSeconds(latestCrossoverEMA.Timestamp).UtcDateTime;

            var istTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            var istDateTime = TimeZoneInfo.ConvertTime(utcDateTime, istTimeZone);
            var result = historicalData.FirstOrDefault(c => c.Time == latestCrossoverEMA.Timestamp);
            // Log the result

            var latestCrossover = new
            {
                Type = latestCrossoverEMA.Type,
                CrossoverTime = istDateTime.ToString("yyyy-MM-dd HH:mm:ss"),
                CrossoverCandle = new
                {
                    Open = result.Open,
                    High = result.High,
                    Low = result.Low,
                    Close = result.Close,
                    Volume = result.Volume
                },
                Angle = latestCrossoverEMA.Angle // Adding the angle to the crossover data
            };
            return Ok(latestCrossover);
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Internal server error");
        }
    }
}

public class HaramiTradingStrategy
{
    public int TotalTradesTaken { get; private set; }
    public int BullishTrades { get; private set; }
    public int BearishTrades { get; private set; }
    public int TargetsHit { get; private set; }
    public int StopLossHit { get; private set; }

    private const double RewardRatio = 2.0;

   
    public static void ExportToCsv(List<TradeData> trades, string filePath)
    {
        using (var writer = new StreamWriter(filePath))
        {
            writer.WriteLine("BodyOverlapPercent,HighLowOverlapPercent,PatternType,Timestamp,Signal,TradeOutcome,EntryPrice,TargetStopPrice,TotalTrades");

            foreach (var trade in trades)
            {
                writer.WriteLine($"{trade.BodyOverlapPercent.ToString(CultureInfo.InvariantCulture)},{trade.HighLowOverlapPercent.ToString(CultureInfo.InvariantCulture)},{trade.PatternType},{trade.Timestamp},{trade.Signal},{trade.TradeOutcome},{trade.EntryPrice.ToString(CultureInfo.InvariantCulture)},{trade.TargetPrice.ToString(CultureInfo.InvariantCulture)},{trade.TotalTrades}");
            }
        }
    }

    public static void ExportToJson(List<Candlestick> trades, string filePath)
    {
        var json = JsonConvert.SerializeObject(trades, Formatting.Indented);
        File.WriteAllText(filePath, json);
    }

    public static void ExportCandlestickToCsv(List<Candlestick> candlesticks, string filePath)
    {
        using (var writer = new StreamWriter(filePath))
        {
            writer.WriteLine("Time,Open,High,Low,Close,Volume");

            foreach (var candlestick in candlesticks)
            {
                writer.WriteLine($"{candlestick.Time},{candlestick.Open.ToString(CultureInfo.InvariantCulture)},{candlestick.High.ToString(CultureInfo.InvariantCulture)},{candlestick.Low.ToString(CultureInfo.InvariantCulture)},{candlestick.Close.ToString(CultureInfo.InvariantCulture)},{candlestick.Volume}");
            }
        }
    }
    public class TradeData
    {
        public double BodyOverlapPercent { get; set; }
        public double HighLowOverlapPercent { get; set; }
        public string PatternType { get; set; }
        public long Timestamp { get; set; }
        public string Signal { get; set; }
        public string TradeOutcome { get; set; }
        public double EntryPrice { get; set; }
        public double TargetPrice { get; set; }
        public double stopLossPrice { get; set; }
        public int TotalTrades { get; set; }
    }

    public class Candlestick
    {
        public long Time { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public long Volume { get; set; }
    }
}


public class HistoricalDataFetcher
{
    public async Task<List<Candlestick>> FetchCandles(string symbol, string resolution, DateTime start, DateTime end)
    {
        using (var client = new HttpClient())
        {
            try
            {
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.3");

                var startTimestamp = new DateTimeOffset(start).ToUnixTimeSeconds();
                var endTimestamp = new DateTimeOffset(end).ToUnixTimeSeconds();

                var url = $"https://cdn.india.deltaex.org/v2/history/candles?resolution={resolution}&symbol={symbol}&start={startTimestamp}&end={endTimestamp}";

                using (var response = await client.GetAsync(url))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        var data = JsonConvert.DeserializeObject<HistoricalData>(json);
                        return ConvertToCandles(data.Result);
                    }
                    else
                    {
                        Console.WriteLine($"Failed to retrieve data. Status code: {response.StatusCode}");
                        var errorContent = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"Error content: {errorContent}");
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                return null;
            }
        }
    }

    private List<Candlestick> ConvertToCandles(List<CandlestickData> rawData)
    {
        var candles = new List<Candlestick>();
        foreach (var item in rawData)
        {
            candles.Add(new Candlestick
            {
                Time = item.Time,
                Open = item.Open,
                High = item.High,
                Low = item.Low,
                Close = item.Close,
                Volume = item.Volume
            });
        }
        return candles;
    }
}



public class Level
{
    public LevelType Type { get; set; }
    public decimal Price { get; set; }
    public long Time { get; set; }
}

public enum LevelType
{
    Support,
    Resistance
}
public class HistoricalData
{
    public List<CandlestickData> Result { get; set; }
}
public class CandlestickData
{
    public long Time { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public long Volume { get; set; }
}

public class MovingAverageAnalyzer
{
    public static Dictionary<string, List<(long Timestamp, decimal Value)>> CalculateMovingAverages(List<Candlestick> candles, int shortTerm, int longTerm)
    {
        var movingAverages = new Dictionary<string, List<(long Timestamp, decimal Value)>>()
        {
            { "Short", new List<(long, decimal)>() },
            { "Long", new List<(long, decimal)>() }
        };

        for (int i = shortTerm - 1; i < candles.Count; i++)
        {
            var shortMA = candles.Skip(i - shortTerm + 1).Take(shortTerm).Average(c => c.Close);
            movingAverages["Short"].Add((candles[i].Time, shortMA));
        }

        for (int i = longTerm - 1; i < candles.Count; i++)
        {
            var longMA = candles.Skip(i - longTerm + 1).Take(longTerm).Average(c => c.Close);
            movingAverages["Long"].Add((candles[i].Time, longMA));
        }

        return movingAverages;
    }

    public static Dictionary<string, List<(long Timestamp, decimal?)>> CalculateAngles(Dictionary<string, List<(long Timestamp, decimal Value)>> movingAverages, int shortTerm, int longTerm)
    {
        var angles = new Dictionary<string, List<(long Timestamp, decimal?)>>()
        {
            { "Short", new List<(long, decimal?)>() },
            { "Long", new List<(long, decimal?)>() }
        };

        if (movingAverages["Short"].Count > 1)
        {
            CalculateAngle(movingAverages["Short"], angles["Short"]);
        }

        if (movingAverages["Long"].Count > 1)
        {
            CalculateAngle(movingAverages["Long"], angles["Long"]);
        }

        return angles;
    }

    private static void CalculateAngle(List<(long Timestamp, decimal Value)> maData, List<(long Timestamp, decimal?)> angleList)
    {
        for (int i = 1; i < maData.Count; i++)
        {
            var x1 = i - 1;
            var y1 = maData[i - 1].Value;
            var x2 = i;
            var y2 = maData[i].Value;

            var slope = (y2 - y1) / (x2 - x1);
            var angle = (decimal)(Math.Atan((double)slope) * (180 / Math.PI));

            angleList.Add((maData[i].Timestamp, angle));
        }
    }

    public static List<(long Timestamp, string Type, decimal Angle)> IdentifyCrossoversAndAngles(
        Dictionary<string, List<(long Timestamp, decimal Value)>> movingAverages,
        Dictionary<string, List<(long Timestamp, decimal?)>> angles)
    {
        var crossovers = new List<(long Timestamp, string Type, decimal Angle)>();

        var maKeys = movingAverages.Keys.ToList();
        if (maKeys.Count < 2)
        {
            return crossovers; // Not enough moving averages to compare
        }

        var ma1 = movingAverages[maKeys[0]];
        var ma2 = movingAverages[maKeys[1]];

        var angle1 = angles.ContainsKey(maKeys[0]) ? angles[maKeys[0]] : new List<(long Timestamp, decimal?)>();
        var angle2 = angles.ContainsKey(maKeys[1]) ? angles[maKeys[1]] : new List<(long Timestamp, decimal?)>();

        var combined = from ma1Item in ma1
                       join ma2Item in ma2 on ma1Item.Timestamp equals ma2Item.Timestamp
                       join a1 in angle1 on ma1Item.Timestamp equals a1.Timestamp into a1Group
                       from a1 in a1Group.DefaultIfEmpty()
                       join a2 in angle2 on ma2Item.Timestamp equals a2.Timestamp into a2Group
                       from a2 in a2Group.DefaultIfEmpty()
                       select new
                       {
                           ma1Item.Timestamp,
                           MA1 = ma1Item.Value,
                           MA2 = ma2Item.Value,
                           Angle1 = a1.Item2 ?? 0m,
                           Angle2 = a2.Item2 ?? 0m
                       };

        // Iterate through the combined data to detect crossovers
        var combinedList = combined.ToList();
        for (int i = 1; i < combinedList.Count; i++)
        {
            var previousItem = combinedList[i - 1];
            var currentItem = combinedList[i];

            bool wasBullish = previousItem.MA1 > previousItem.MA2;
            bool isBullish = currentItem.MA1 > currentItem.MA2;

            if (wasBullish && !isBullish)
            {
                // Bearish crossover detected
                crossovers.Add((currentItem.Timestamp, "Bearish", (currentItem.Angle1 + currentItem.Angle2) / 2));
            }
            else if (!wasBullish && isBullish)
            {
                // Bullish crossover detected
                crossovers.Add((currentItem.Timestamp, "Bullish", (currentItem.Angle1 + currentItem.Angle2) / 2));
            }
        }

        return crossovers;
    }




    public class Trade
    {
        public long EntryTimestamp { get; set; }
        public decimal EntryPrice { get; set; }
        public decimal StopLoss { get; set; }
        public decimal Target { get; set; }
        public long ExitTimestamp { get; set; }
        public decimal ExitPrice { get; set; }
        public decimal ProfitLoss => ExitPrice - EntryPrice;
        public bool TargetHit { get; set; }
        public bool StopLossHit { get; set; }
    }
}


public class VolumeDivergence
{
    private readonly int _lookBackPeriod;
    private readonly decimal _volumeThreshold; // Percentage difference threshold for volume comparison

    public VolumeDivergence(int lookBackPeriod = 3, decimal volumeThreshold = 0.10m)
    {
        _lookBackPeriod = lookBackPeriod;
        _volumeThreshold = volumeThreshold;
    }

    // Detects bullish volume divergence
    public bool IsBullishVolumeDivergence(List<Candlestick> candlesticks)
    {
        if (!IsValidCandlestickList(candlesticks)) return false;

        int lastIndex = candlesticks.Count - 1;

        for (int i = lastIndex - _lookBackPeriod; i < lastIndex; i++)
        {
            //if (candlesticks[i].Low > candlesticks[lastIndex].Low &&
            //   IsSignificantVolumeDifference(candlesticks[i].Volume, candlesticks[lastIndex].Volume))


            if (candlesticks[i].Low < candlesticks[lastIndex].Low &&
            IsSignificantVolumeDifference(candlesticks[i].Volume, candlesticks[lastIndex].Volume))
            {
                return true;
            }
        }
        return false;
    }

    // Detects bearish volume divergence
    public bool IsBearishVolumeDivergence(List<Candlestick> candlesticks)
    {
        if (!IsValidCandlestickList(candlesticks)) return false;

        int lastIndex = candlesticks.Count - 1;

        for (int i = lastIndex - _lookBackPeriod; i < lastIndex; i++)
        {
            // if (candlesticks[i].High < candlesticks[lastIndex].High &&
            //    IsSignificantVolumeDifference(candlesticks[i].Volume, candlesticks[lastIndex].Volume))


            if (candlesticks[i].High > candlesticks[lastIndex].High &&
            IsSignificantVolumeDifference(candlesticks[i].Volume, candlesticks[lastIndex].Volume))
            {
                return true;
            }
        }
        return false;
    }
    // Detects bullish volume divergence
    //public bool IsBullishVolumeDivergence(List<Candlestick> candlesticks)
    //{
    //    // Ensure the list has enough candlesticks
    //    if (!IsValidCandlestickList(candlesticks)) return false;

    //    int lastIndex = candlesticks.Count - 1;

    //    // Look for bullish divergence by comparing lows and volume
    //    for (int i = lastIndex - _lookBackPeriod; i < lastIndex; i++)
    //    {
    //        // Condition for bullish divergence:
    //        // A higher low with a significant increase in volume
    //        if (candlesticks[i].Low > candlesticks[lastIndex].Low &&
    //            IsSignificantVolumeDifference(candlesticks[i].Volume, candlesticks[lastIndex].Volume))
    //        {
    //            return true;
    //        }
    //    }

    //    return false;
    //}

    //// Detects bearish volume divergence
    //public bool IsBearishVolumeDivergence(List<Candlestick> candlesticks)
    //{
    //    // Ensure the list has enough candlesticks
    //    if (!IsValidCandlestickList(candlesticks)) return false;

    //    int lastIndex = candlesticks.Count - 1;

    //    // Look for bearish divergence by comparing highs and volume
    //    for (int i = lastIndex - _lookBackPeriod; i < lastIndex; i++)
    //    {
    //        // Condition for bearish divergence:
    //        // A lower high with a significant increase in volume
    //        if (candlesticks[i].High < candlesticks[lastIndex].High &&
    //            IsSignificantVolumeDifference(candlesticks[i].Volume, candlesticks[lastIndex].Volume))
    //        {
    //            return true;
    //        }
    //    }

    //    return false;
    //}

    // Check if the volume difference is significant based on the threshold
    private bool IsSignificantVolumeDifference(decimal previousVolume, decimal currentVolume)
    {
        decimal volumeChange = Math.Abs(previousVolume - currentVolume) / previousVolume;
        return volumeChange >= _volumeThreshold;
    }

    // Validate if candlestick list is valid for divergence analysis
    private bool IsValidCandlestickList(List<Candlestick> candlesticks)
    {
        return candlesticks != null && candlesticks.Count >= _lookBackPeriod + 1;
    }
}





