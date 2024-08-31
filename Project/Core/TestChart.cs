//using Microsoft.AspNetCore.Mvc;
//using Newtonsoft.Json;
//using System;
//using System.Collections.Generic;
//using System.Net.Http;
//using System.Net.Http.Headers;
//using System.Threading.Tasks;
//using System.Linq;
//using static HaramiTradingStrategy;
//using ScottPlot.Finance;
//using System.Globalization;
//using System.Diagnostics;
//using System.Reflection.Metadata;
//using System.Runtime.InteropServices;

//[ApiController]
//[Route("[controller]")]
//public class TradingController : ControllerBase
//{
//    [HttpGet(Name = "GetTest")]
//    public async Task<IActionResult> GetTest()
//    {
//        var fetcher = new HistoricalDataFetcher();
//        var resolution = "5m";
//        var startDate = DateTime.UtcNow.AddMinutes(-10000*15);
//        //var startDate = DateTime.UtcNow.AddMinutes(-2000 *5); // 2000 minutes ago
//        var endDate = DateTime.UtcNow;
//        var symbol = "BTCUSD";
//        var maxInterval = 10000*15;
//        var start = 2;
//        var end = 1;
//         List<Candlestick> historicalData = await fetcher.FetchCandles(symbol, resolution, startDate, endDate);
//        //List<Candlestick> historicalData = new List<Candlestick>();
//        //while (startDate > new DateTime(2022, 1, 1))
//        //{
//        //    var chunkData = await fetcher.FetchCandles(symbol, resolution, startDate, endDate);
//        //    if (chunkData == null)
//        //    {
//        //        return StatusCode(500, "Failed to retrieve historical data chunk.");
//        //    }
//        //    historicalData.AddRange(chunkData);

//        //    // Adjust dates for the next chunk backwards
//        //     // Adjust endDate to start of previous chunk
//        //    startDate = DateTime.UtcNow.AddMinutes(-(maxInterval* start));
//        //    endDate = DateTime.UtcNow.AddMinutes(-(maxInterval * (end)));// Adjust startDate for the next chunk
//        //    start++;
//        //    end++;
//        //}

//        // Reverse the data to get it in ascending chronological order
//        historicalData.Reverse();

//        // Return the fetched historical data
//        // return Ok();


//        //  ExportCandlestickToCsv(historicalData, "historicalData.csv");
//        // Export to JSON
//        //ExportToJson(historicalData, "historicalData.json");

//        //if (historicalData == null)
//        //{
//        //    return StatusCode(500, "Failed to retrieve historical data.");
//        //}
//        //else
//        //{
//        //    historicalData.Reverse();
//        //}

//        int shortTerm = 5;
//        int longTerm = 15;
//        decimal stopLossPercentage = 1; // Example stop loss of 2%
//        decimal targetPercentage = 2;

//        //var trades = MovingAverageAnalyzer.BacktestStrategy(historicalData, shortTerm, longTerm, stopLossPercentage, targetPercentage);

//        //Console.WriteLine("Entry Timestamp | Entry Price | Exit Timestamp | Exit Price | Profit/Loss | Target Hit | Stop Loss Hit");
//        //foreach (var trade in trades)
//        //{
//        //    var entryDate = DateTimeOffset.FromUnixTimeSeconds(trade.EntryTimestamp).DateTime;
//        //    var exitDate = DateTimeOffset.FromUnixTimeSeconds(trade.ExitTimestamp).DateTime;
//        //    Console.WriteLine($"{entryDate.ToShortDateString()} | {trade.EntryPrice} | {exitDate.ToShortDateString()} | {trade.ExitPrice} | {trade.ProfitLoss} | {trade.TargetHit} | {trade.StopLossHit}");
//        //}








//        var movingAverages = MovingAverageAnalyzer.CalculateMovingAverages(historicalData, shortTerm, longTerm);
//        var angles = MovingAverageAnalyzer.CalculateAngles(movingAverages, shortTerm, longTerm);
//        var crossovers = MovingAverageAnalyzer.IdentifyCrossoversAndAngles(movingAverages, angles, shortTerm, longTerm);



//        //Output results
//        Console.WriteLine("Date       | Type   | Average Angle");
//        foreach (var crossover in crossovers)
//        {
//            Console.WriteLine($"{crossover.Timestamp} | {crossover.Type} | {crossover.Angle}°");
//        }

//        // HaramiTradingStrategy strategy = new HaramiTradingStrategy();
//        // var data = strategy.RunStrategy(historicalData);

//        // ExportToCsv(data, "trades.csv");
//        //var input = new TradingBand.InputParameters
//        //{
//        //    Src = historicalData.Select(c => (double)c.Close).ToList(),
//        //    Mode = TradingBand.ModeSwitch.Hma,
//        //    Length = 55,
//        //    LengthMult = 1.0,
//        //    UseHtf = false,
//        //    Htf = 240,
//        //    SwitchColor = true,
//        //    CandleCol = false,
//        //    VisualSwitch = true,
//        //    ThicknesSwitch = 1,
//        //    TranspSwitch = 40
//        //};


//        // var (MHULL, SHULL) = TradingBand.ComputeTradingBand(input);
//        //  TradingSignals.IdentifyBuySellSignals(historicalData);

//        //// Calculate trend and trend angle
//        //TrendAnalysis.CalculateTrend(MHULL);
//        //TrendAnalysis.CalculateTrendAngle(MHULL);
//        //// Output or plot MHULL and SHULL as needed
//        ////Console.WriteLine("MHULL:");
//        ////foreach (var value in MHULL)
//        ////{
//        ////    Console.WriteLine(value);
//        ////}

//        ////Console.WriteLine("SHULL:");
//        ////foreach (var value in SHULL)
//        ////{
//        ////    Console.WriteLine(value);
//        ////}
//        return Ok();
//    }
//}

//public class HaramiTradingStrategy
//{
//    public int TotalTradesTaken { get; private set; }
//    public int BullishTrades { get; private set; }
//    public int BearishTrades { get; private set; }
//    public int TargetsHit { get; private set; }
//    public int StopLossHit { get; private set; }

//    private const double RewardRatio = 2.0;

//    public List<TradeData> RunStrategy(List<Candlestick> historicalData)
//    {
//        string trade = null;
//        long entrytime = 0;
//        double entryPrice = 0;
//        double stopLossPrice = 0;
//        double targetPrice = 0;
//        bool istrade = false;

//        (double bodyOverlapPercentage, double highLowOverlapPercentage) = (0.0, 0.0);


//        List<TradeData> tradeData = new List<TradeData>();

//        for (int i = 0; i < historicalData.Count - 2; i++)
//        {
//            Candlestick current = historicalData[i];
//            Candlestick next = historicalData[i + 1];
//            Candlestick thirdCandle = historicalData[i + 2];


//            //Console.WriteLine($"Volume {next.volume}");
//            if (!istrade)
//            {
//                // Check for bullish Harami
//                if (IsBullishHarami(current, next, thirdCandle))
//                {
//                    BullishTrades++;
//                    TotalTradesTaken++;
//                    Console.WriteLine($"Bullish Harami identified on {current.Time}: Buy signal");

//                    // Implement buy logic
//                    entryPrice = (double)current.Close;
//                    stopLossPrice = (double)current.Low - (double)(current.High - current.Low);
//                    targetPrice = entryPrice + RewardRatio * (entryPrice - stopLossPrice);
//                    entrytime = next.Time;
//                    trade = "Bullish";
//                    istrade = true;

//                    if (trade == "Bullish")++++
//                    {
//                        CandlestickAnalyzer analyzer = new CandlestickAnalyzer();
//                        (bodyOverlapPercentage, highLowOverlapPercentage) = analyzer.CalculateOverlapPercentage(current, next);

//                        Console.WriteLine($"bodyOverlapPercentage: {bodyOverlapPercentage},highLowOverlapPercentage {highLowOverlapPercentage}");
//                    }
//                    // Check if target is hit with next candle

//                }
//                // Check for bearish Harami
//                else if (IsBearishHarami(current, next, thirdCandle))
//                {
//                    BearishTrades++;
//                    TotalTradesTaken++;
//                    Console.WriteLine($"Bearish Harami identified on {current.Time}: Sell signal");

//                    // Implement sell logic
//                    entryPrice = (double)current.Close;
//                    stopLossPrice = (double)current.High + (double)(current.High - current.Low);
//                    targetPrice = entryPrice - RewardRatio * (stopLossPrice - entryPrice);
//                    entrytime = next.Time;
//                    trade = "Bearish";
//                    istrade = true;
//                    // Check if target is hit with next candle
//                    if (trade == "Bearish")
//                    {
//                        CandlestickAnalyzer analyzer = new CandlestickAnalyzer();
//                        (bodyOverlapPercentage, highLowOverlapPercentage) = analyzer.CalculateOverlapPercentage(current, next);

//                        Console.WriteLine($"bodyOverlapPercentage: {bodyOverlapPercentage},highLowOverlapPercentage {highLowOverlapPercentage}");
//                    }
//                }

//            }

//            if (!string.IsNullOrEmpty(trade) && trade == "Bearish" && entrytime < next.Time && istrade)
//            {
//                if ((double)next.Low <= targetPrice && entrytime < next.Time)
//                {
//                    TargetsHit++;

                   
//                    var currntTrade = new TradeData
//                    {
//                        BodyOverlapPercent = bodyOverlapPercentage,
//                        HighLowOverlapPercent = highLowOverlapPercentage,
//                        EntryPrice = entryPrice,
//                        TargetPrice = targetPrice,
//                        stopLossPrice=stopLossPrice,
//                        TradeOutcome = "Win",
//                        PatternType = trade,
//                        Timestamp = entrytime,
//                        TotalTrades = TotalTradesTaken
//                    };

//                    tradeData.Add(currntTrade);
//                    Console.WriteLine($"Target hit on {next.Time}. Trade outcome: Win - {TargetsHit}--{TotalTradesTaken}- entryPrice{entryPrice}-- targ-{targetPrice} ");
//                    istrade = false;
//                }
//                else if ((double)next.High >= stopLossPrice && entrytime < next.Time)
//                {
//                    StopLossHit++;
//                    Console.WriteLine($"Stop-loss hit on {next.Time}. Trade outcome: Loss- {StopLossHit}-total{TotalTradesTaken}-- entryPrice{entryPrice}--{targetPrice}");
//                    istrade = false;
//                }
//            }
//            else if (!string.IsNullOrEmpty(trade) && trade == "Bullish" && entrytime < next.Time && istrade)
//            {
//                if (((double)next.High) >= targetPrice && entrytime < next.Time)
//                {
//                    TargetsHit++;
//                    var currntTrade = new TradeData
//                    {
//                        BodyOverlapPercent = bodyOverlapPercentage,
//                        HighLowOverlapPercent = highLowOverlapPercentage,
//                        EntryPrice = entryPrice,
//                        TargetPrice = targetPrice,
//                        stopLossPrice = stopLossPrice,
//                        TradeOutcome = "Win",
//                        PatternType = trade,
//                        Timestamp = entrytime,
//                        TotalTrades = TotalTradesTaken

//                    };
//                    Console.WriteLine($"Target hit on {next.Time}. Trade outcome: Win - {TargetsHit}--{TotalTradesTaken}- entryPrice{entryPrice}--Targ-{targetPrice}");
//                    istrade = false;
//                }
//                else if ((double)next.Low <= stopLossPrice && entrytime < next.Time)
//                {
//                    StopLossHit++;
//                    Console.WriteLine($"Stop-loss hit on {next.Time}. Trade outcome: Loss- {StopLossHit}-total{TotalTradesTaken}-- entryPrice{entryPrice}--{targetPrice}");
//                    istrade = false;
//                }
//            }

//        }
//      return tradeData;
//    }


//    private bool IsBullishHarami(Candlestick current, Candlestick next, Candlestick thirdCandle)
//    {
//        //var isBullishHarami = current.Close < current.Open && // Current candle is green
//        //         next.Close > next.Open &&       // Next candle is red
//        //         next.Open > current.Close &&    // Next candle opens within current candle's body
//        //         next.Close < current.Open;


//        var isBody = (current.Close < current.Open || current.Close > current.Open) && // Current candle is green
//                (next.Close > next.Open|| next.Close < next.Open) &&       // Next candle is red
//                (next.Open > current.Close || next.Open < current.Close) &&    // Next candle opens within current candle's body
//                (next.Close < current.Open || next.Close < current.Open);

//        var isBullishHarami = isBody && next.High < thirdCandle.Close;

 
//        //var isBullishHarami = current.Close < current.Open && // Current candle is green
//        //         next.Close > next.Open &&       // Next candle is red
//        //         next.Open > current.Close &&    // Next candle opens within current candle's body
//        //         next.Close < current.Open;
//        //if (isBullishHarami)
//        //{
//        //    CandlestickAnalyzer analyzer = new CandlestickAnalyzer();
//        //    var (bodyOverlapPercentage, highLowOverlapPercentage) = analyzer.CalculateOverlapPercentage(current, next);

//        //    Console.WriteLine($"bodyOverlapPercentage: {bodyOverlapPercentage},highLowOverlapPercentage {highLowOverlapPercentage}");
//        //}
//        // Bullish Harami: Current candle is bullish (green) and next candle is bearish (red)
//        return isBullishHarami;      // Next candle closes within current candle's body
//    }


//    private bool IsBearishHarami(Candlestick current, Candlestick next, Candlestick thirdCandle)
//    {
//        // Bearish Harami: Current candle is bearish (red) and next candle is bullish (green)
//        //var isBearishHarami = current.Close > current.Open && // Current candle is red
//        //        next.Close < next.Open &&       // Next candle is green
//        //        next.Open < current.Close &&    // Next candle opens within current candle's body
//        //        next.Close > current.Open;


//        var isBody = (current.Close < current.Open || current.Close > current.Open) && // Current candle is green
//        (next.Close > next.Open || next.Close < next.Open) &&       // Next candle is red
//        (next.Open > current.Close || next.Open < current.Close) &&    // Next candle opens within current candle's body
//        (next.Close < current.Open || next.Close < current.Open);

//        var isBearishHarami = isBody && next.Low < thirdCandle.Close;
//        //if (isBearishHarami)
//        //{
//        //    isBearishHarami = next.Low < thirdCandle.Close;
//        //}

//        //if (isBearishHarami)
//        //{
//        //    CandlestickAnalyzer analyzer = new CandlestickAnalyzer();
//        //    var (bodyOverlapPercentage, highLowOverlapPercentage) = analyzer.CalculateOverlapPercentage(current, next);

//        //    Console.WriteLine($"bodyOverlapPercentage: {bodyOverlapPercentage},highLowOverlapPercentage {highLowOverlapPercentage}");
//        //}

//        return isBearishHarami;      // Next candle closes within current candle's body
//    }
//    public static void ExportToCsv(List<TradeData> trades, string filePath)
//    {
//        using (var writer = new StreamWriter(filePath))
//        {
//            writer.WriteLine("BodyOverlapPercent,HighLowOverlapPercent,PatternType,Timestamp,Signal,TradeOutcome,EntryPrice,TargetStopPrice,TotalTrades");

//            foreach (var trade in trades)
//            {
//                writer.WriteLine($"{trade.BodyOverlapPercent.ToString(CultureInfo.InvariantCulture)},{trade.HighLowOverlapPercent.ToString(CultureInfo.InvariantCulture)},{trade.PatternType},{trade.Timestamp},{trade.Signal},{trade.TradeOutcome},{trade.EntryPrice.ToString(CultureInfo.InvariantCulture)},{trade.TargetPrice.ToString(CultureInfo.InvariantCulture)},{trade.TotalTrades}");
//            }
//        }
//    }

//    public static void ExportToJson(List<Candlestick> trades, string filePath)
//    {
//        var json = JsonConvert.SerializeObject(trades, Formatting.Indented);
//        File.WriteAllText(filePath, json);
//    }

//    public static void ExportCandlestickToCsv(List<Candlestick> candlesticks, string filePath)
//    {
//        using (var writer = new StreamWriter(filePath))
//        {
//            writer.WriteLine("Time,Open,High,Low,Close,Volume");

//            foreach (var candlestick in candlesticks)
//            {
//                writer.WriteLine($"{candlestick.Time},{candlestick.Open.ToString(CultureInfo.InvariantCulture)},{candlestick.High.ToString(CultureInfo.InvariantCulture)},{candlestick.Low.ToString(CultureInfo.InvariantCulture)},{candlestick.Close.ToString(CultureInfo.InvariantCulture)},{candlestick.Volume}");
//            }
//        }
//    }
//    public class TradeData
//    {
//        public double BodyOverlapPercent { get; set; }
//        public double HighLowOverlapPercent { get; set; }
//        public string PatternType { get; set; }
//        public long Timestamp { get; set; }
//        public string Signal { get; set; }
//        public string TradeOutcome { get; set; }
//        public double EntryPrice { get; set; }
//        public double TargetPrice { get; set; }
//        public double stopLossPrice { get; set; }
//        public int TotalTrades { get; set; }
//    }

//    public class Candlestick
//    {
//        public long Time { get; set; }
//        public decimal Open { get; set; }
//        public decimal High { get; set; }
//        public decimal Low { get; set; }
//        public decimal Close { get; set; }
//        public long Volume { get; set; }
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
//    public long Volume { get; set; }
//}
//public class TradingBand
//{
//    public enum ModeSwitch { Hma, Ehma, Thma }

//    public class InputParameters
//    {
//        public List<double> Src { get; set; }
//        public ModeSwitch Mode { get; set; }
//        public int Length { get; set; }
//        public double LengthMult { get; set; }
//        public bool UseHtf { get; set; }
//        public int Htf { get; set; }
//        public bool SwitchColor { get; set; }
//        public bool CandleCol { get; set; }
//        public bool VisualSwitch { get; set; }
//        public int ThicknesSwitch { get; set; }
//        public int TranspSwitch { get; set; }
//    }

//    public static List<double> WMA(List<double> src, int length)
//    {
//        List<double> wma = new List<double>();
//        double norm = length * (length + 1) / 2.0;
//        for (int i = length - 1; i < src.Count; i++)
//        {
//            double sum = 0;
//            for (int j = 0; j < length; j++)
//            {
//                sum += src[i - j] * (length - j);
//            }
//            wma.Add(sum / norm);
//        }
//        return wma;
//    }

//    public static List<double> EMA(List<double> src, int length)
//    {
//        List<double> ema = new List<double>();
//        double multiplier = 2.0 / (length + 1);
//        ema.Add(src[0]); // Initialize with the first value
//        for (int i = 1; i < src.Count; i++)
//        {
//            double value = (src[i] - ema[i - 1]) * multiplier + ema[i - 1];
//            ema.Add(value);
//        }
//        return ema;
//    }

//    public static List<double> HMA(List<double> src, int length)
//    {
//        List<double> halfWma = WMA(src, length / 2);
//        List<double> fullWma = WMA(src, length);
//        List<double> diff = new List<double>();
//        for (int i = 0; i < fullWma.Count; i++)
//        {
//            diff.Add(2 * halfWma[i] - fullWma[i]);
//        }
//        return WMA(diff, (int)Math.Round(Math.Sqrt(length)));
//    }

//    public static List<double> EHMA(List<double> src, int length)
//    {
//        List<double> ema1 = EMA(src, length);
//        List<double> ema2 = EMA(ema1, length);
//        List<double> diff = new List<double>();
//        for (int i = 0; i < ema1.Count; i++)
//        {
//            diff.Add(2 * ema1[i] - ema2[i]);
//        }
//        return EMA(diff, (int)Math.Round(Math.Sqrt(length)));
//    }

//    public static List<double> THMA(List<double> src, int length)
//    {
//        List<double> wma1 = WMA(src, length / 3);
//        List<double> wma2 = WMA(src, length / 2);
//        List<double> wma3 = WMA(src, length);
//        List<double> diff = new List<double>();
//        for (int i = 0; i < wma1.Count; i++)
//        {
//            diff.Add(3 * wma1[i] - wma2[i] - wma3[i]);
//        }
//        return WMA(diff, length);
//    }

//    public static List<double> Mode(ModeSwitch modeSwitch, List<double> src, int length)
//    {
//        switch (modeSwitch)
//        {
//            case ModeSwitch.Hma:
//                return HMA(src, length);
//            case ModeSwitch.Ehma:
//                return EHMA(src, length);
//            case ModeSwitch.Thma:
//                return THMA(src, length);
//            default:
//                throw new ArgumentException("Invalid mode switch");
//        }
//    }

//    public static (List<double>, List<double>) ComputeTradingBand(InputParameters input)
//    {
//        int length = (int)(input.Length * input.LengthMult);
//        List<double> hull = Mode(input.Mode, input.Src, length);

//        // Here you can add the higher timeframe (HTF) logic if needed

//        // Color and other parameters can be calculated here as needed
//        // Example: if hull[i] > hull[i-2], color.green else color.red

//        return (hull, hull.Skip(2).ToList());
//    }
//}
//public class MovingAverageAnalyzer
//{
//    // Method to calculate moving averages
//    public static Dictionary<string, List<(long Timestamp, decimal Value)>> CalculateMovingAverages(List<Candlestick> candles, int shortTerm, int longTerm)
//    {
//        var movingAverages = new Dictionary<string, List<(long Timestamp, decimal Value)>>()
//        {
//            { "Short", new List<(long, decimal)>() },
//            { "Long", new List<(long, decimal)>() }
//        };

//        for (int i = shortTerm - 1; i < candles.Count; i++)
//        {
//            var shortMA = candles.Skip(i - shortTerm + 1).Take(shortTerm).Average(c => c.Close);
//            movingAverages["Short"].Add((candles[i].Time, shortMA));
//        }

//        for (int i = longTerm - 1; i < candles.Count; i++)
//        {
//            var longMA = candles.Skip(i - longTerm + 1).Take(longTerm).Average(c => c.Close);
//            movingAverages["Long"].Add((candles[i].Time, longMA));
//        }

//        return movingAverages;
//    }

//    // Method to calculate angles
//    public static Dictionary<string, List<(long Timestamp, decimal Value)>> CalculateAngles(Dictionary<string, List<(long Timestamp, decimal Value)>> movingAverages, int shortTerm, int longTerm)
//    {
//        var angles = new Dictionary<string, List<(long Timestamp, decimal Value)>>()
//        {
//            { "Short", new List<(long, decimal)>() },
//            { "Long", new List<(long, decimal)>() }
//        };

//        if (movingAverages["Short"].Count > 1)
//        {
//            CalculateAngle(movingAverages["Short"], angles["Short"]);
//        }

//        if (movingAverages["Long"].Count > 1)
//        {
//            CalculateAngle(movingAverages["Long"], angles["Long"]);
//        }

//        return angles;
//    }

//    // Method to calculate the angle
//    private static void CalculateAngle(List<(long Timestamp, decimal Value)> maData, List<(long Timestamp, decimal Value)> angleList)
//    {
//        for (int i = 1; i < maData.Count; i++)
//        {
//            var x1 = i - 1;
//            var y1 = maData[i - 1].Value;
//            var x2 = i;
//            var y2 = maData[i].Value;

//            var slope = (y2 - y1) / (x2 - x1);
//            var angle = (decimal)(Math.Atan((double)slope) * (180 / Math.PI));

//            angleList.Add((maData[i].Timestamp, angle));
//        }
//    }

//    // Method to identify crossovers and check angles
//    public static List<(long Timestamp, string Type, decimal Angle)> IdentifyCrossoversAndAngles(Dictionary<string, List<(long Timestamp, decimal Value)>> movingAverages, Dictionary<string, List<(long Timestamp, decimal Value)>> angles, int shortTerm, int longTerm)
//    {
//        var crossovers = new List<(long Timestamp, string Type, decimal Angle)>();

//        var shortMAs = movingAverages["Short"];
//        var longMAs = movingAverages["Long"];
//        var shortAngles = angles["Short"];
//        var longAngles = angles["Long"];

//        var combined = from s in shortMAs
//                       join l in longMAs on s.Timestamp equals l.Timestamp
//                       join sa in shortAngles on s.Timestamp equals sa.Timestamp
//                       join la in longAngles on l.Timestamp equals la.Timestamp
//                       select new { s.Timestamp, ShortMA = s.Value, LongMA = l.Value, ShortAngle = sa.Value, LongAngle = la.Value };

//        foreach (var item in combined)
//        {
//            string crossoverType = "None";

//            if (item.ShortMA > item.LongMA && item.ShortAngle > 20 && item.LongAngle > 20)
//            {
//                crossoverType = "Bullish";
//            }
//            else if (item.ShortMA < item.LongMA && item.ShortAngle > 20 && item.LongAngle > 20)
//            {
//                crossoverType = "Bearish";
//            }

//            if (crossoverType != "None")
//            {
//                crossovers.Add((item.Timestamp, crossoverType, (item.ShortAngle + item.LongAngle) / 2)); // Average angle for both MAs
//            }
//        }

//        return crossovers;
//    }

//    public static List<Trade> BacktestStrategy(List<Candlestick> candles, int shortTerm, int longTerm, decimal stopLossPercentage, decimal targetPercentage)
//    {
//        var movingAverages = CalculateMovingAverages(candles, shortTerm, longTerm);
//        var angles = CalculateAngles(movingAverages, shortTerm, longTerm);
//        var crossovers = IdentifyCrossoversAndAngles(movingAverages, angles, shortTerm, longTerm);

//        var trades = new List<Trade>();
//        Trade currentTrade = null;

//        foreach (var crossover in crossovers)
//        {
//            if (crossover.Type == "Bullish")
//            {
//                if (currentTrade == null)
//                {
//                    var entryPrice = candles.First(c => c.Time == crossover.Timestamp).Close;
//                    var stopLoss = entryPrice * (1 - stopLossPercentage / 100);
//                    var target = entryPrice * (1 + targetPercentage / 100);

//                    currentTrade = new Trade
//                    {
//                        EntryTimestamp = crossover.Timestamp,
//                        EntryPrice = entryPrice,
//                        StopLoss = stopLoss,
//                        Target = target
//                    };
//                }
//            }
//            else if (crossover.Type == "Bearish")
//            {
//                if (currentTrade != null)
//                {
//                    // Close trade at crossover if no stop loss or target hit
//                    currentTrade.ExitTimestamp = crossover.Timestamp;
//                    currentTrade.ExitPrice = candles.First(c => c.Time == crossover.Timestamp).Close;
//                    trades.Add(currentTrade);
//                    currentTrade = null;
//                }
//            }
//        }

//        // Handle remaining trades if open
//        if (currentTrade != null)
//        {
//            var exitCandle = candles.Last(); // Use last candle if still open
//            currentTrade.ExitTimestamp = exitCandle.Time;
//            currentTrade.ExitPrice = exitCandle.Close;
//            trades.Add(currentTrade);
//        }

//        // Check stop loss and target hits
//        foreach (var trade in trades)
//        {
//            var entryIndex = candles.FindIndex(c => c.Time == trade.EntryTimestamp);
//            var exitIndex = candles.FindIndex(c => c.Time == trade.ExitTimestamp);

//            for (int i = entryIndex; i <= exitIndex; i++)
//            {
//                var candle = candles[i];
//                if (candle.Close <= trade.StopLoss)
//                {
//                    trade.StopLossHit = true;
//                    trade.ExitPrice = trade.StopLoss; // Set exit price to stop loss price
//                    break;
//                }

//                if (candle.Close >= trade.Target)
//                {
//                    trade.TargetHit = true;
//                    trade.ExitPrice = trade.Target; // Set exit price to target price
//                    break;
//                }
//            }
//        }

//        return trades;
//    }
//}
//public class Trade
//{
//    public long EntryTimestamp { get; set; }
//    public decimal EntryPrice { get; set; }
//    public decimal StopLoss { get; set; }
//    public decimal Target { get; set; }
//    public long ExitTimestamp { get; set; }
//    public decimal ExitPrice { get; set; }
//    public decimal ProfitLoss => ExitPrice - EntryPrice;
//    public bool TargetHit { get; set; }
//    public bool StopLossHit { get; set; }
//}


//public static class TrendAnalysis
//{
//    public static void CalculateTrend(List<double> hull)
//    {
//        // Example: Calculate trend direction based on the last two points of hull
//        if (hull.Count >= 2)
//        {
//            double lastValue = hull[hull.Count - 1];
//            double secondLastValue = hull[hull.Count - 2];

//            if (lastValue > secondLastValue)
//            {
//                Console.WriteLine("Current trend: Up");
//            }
//            else if (lastValue < secondLastValue)
//            {
//                Console.WriteLine("Current trend: Down");
//            }
//            else
//            {
//                Console.WriteLine("Current trend: Flat");
//            }
//        }
//        else
//        {
//            Console.WriteLine("Insufficient data to calculate trend");
//        }
//    }

//    public static void CalculateTrendAngle(List<double> hull)
//    {
//        // Example: Calculate trend angle based on the slope of hull
//        if (hull.Count >= 2)
//        {
//            double lastValue = hull[hull.Count - 1];
//            double secondLastValue = hull[hull.Count - 2];

//            double trendAngle = Math.Atan((lastValue - secondLastValue) / 1) * (180 / Math.PI); // Calculate angle in degrees

//            Console.WriteLine($"Current trend angle: {trendAngle} degrees");
//        }
//        else
//        {
//            Console.WriteLine("Insufficient data to calculate trend angle");
//        }
//    }
//}


//public static class TradingSignals
//{
//    public static void IdentifyBuySellSignals(List<Candlestick> historicalData)
//    {
//        // Initialize your trading strategy
//        HaramiTradingStrategy strategy = new HaramiTradingStrategy();

//        // Run the strategy on historical data to identify signals
//        strategy.RunStrategy(historicalData);

//        // Access the results from the strategy
//        int totalTradesTaken = strategy.TotalTradesTaken;
//        int bullishTrades = strategy.BullishTrades;
//        int bearishTrades = strategy.BearishTrades;
//        int targetsHit = strategy.TargetsHit;
//        int stopLossHit = strategy.StopLossHit;

//        // You can print or process these results as needed
//        Console.WriteLine($"Total Trades Taken: {totalTradesTaken}");
//        Console.WriteLine($"Bullish Trades: {bullishTrades}");
//        Console.WriteLine($"Bearish Trades: {bearishTrades}");
//        Console.WriteLine($"Targets Hit: {targetsHit}");
//        Console.WriteLine($"Stop Losses Hit: {stopLossHit}");

//        // Implement additional logic based on your trading strategy for further signal processing
//    }
//}
//public class CandlestickAnalyzer
//{
//    public (double BodyOverlapPercentage, double HighLowOverlapPercentage) CalculateOverlapPercentage(Candlestick current, Candlestick next)
//    {
//        // Calculate the range of the candle bodies
//        double currentBodyRange = (double)Math.Abs(current.Close - current.Open);
//        double nextBodyRange = (double)Math.Abs(next.Close - next.Open);

//        // Calculate the high-low range
//        double currentHighLowRange = (double)(current.High - current.Low);
//        double nextHighLowRange = (double)(next.High - next.Low);

//        // Calculate the start and end of the current candle body
//        double currentBodyStart = (double)Math.Min(current.Open, current.Close);
//        double currentBodyEnd = (double)Math.Max(current.Open, current.Close);

//        // Calculate the start and end of the next candle body
//        double nextBodyStart = (double)Math.Min(next.Open, next.Close);
//        double nextBodyEnd = (double)Math.Max(next.Open, next.Close);

//        // Calculate body overlap range
//        double bodyOverlapStart = Math.Max(currentBodyStart, nextBodyStart);
//        double bodyOverlapEnd = Math.Min(currentBodyEnd, nextBodyEnd);
//        double bodyOverlapRange = Math.Max(0, bodyOverlapEnd - bodyOverlapStart); // Ensure overlapRange is non-negative

//        // Calculate percentage overlap based on the current candle's body range
//        double bodyOverlapPercentage = (bodyOverlapRange / currentBodyRange) * 100;

//        // Calculate the start and end of the high-low range overlap
//        double overlapStart = (double)Math.Max(current.Low, next.Low);
//        double overlapEnd = (double)Math.Min(current.High, next.High);
//        double highLowOverlapRange = Math.Max(0, overlapEnd - overlapStart); // Ensure overlapRange is non-negative

//        // Calculate percentage overlap based on the current candle's high-low range
//        double highLowOverlapPercentage = (highLowOverlapRange / currentHighLowRange) * 100;

//        return (bodyOverlapPercentage, highLowOverlapPercentage);
//    }
//}

