using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using static Test_Project.Controllers.ControlController;
using System.Reflection.Metadata;
using Newtonsoft.Json.Linq;
using static System.Collections.Specialized.BitVector32;
using Serilog;
using static HaramiTradingStrategy;

public class PeriodicTaskService : BackgroundService
{
    private readonly ILogger<PeriodicTaskService> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    //private bool _isRunning = true;
    //private CancellationTokenSource _cts;
    private readonly ITaskStateService _taskStateService;
    private readonly IConfiguration _appSettings;

    public PeriodicTaskService(ILogger<PeriodicTaskService> logger, IServiceScopeFactory serviceScopeFactory, ITaskStateService taskStateService, IConfiguration configuration)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
        _taskStateService = taskStateService;
        _appSettings = configuration;
    }

    private TimeSpan _interval = TimeSpan.FromMinutes(5);
    private bool _isRunning = false;
    private readonly object _lock = new object();
    string logTime;
    private long count = 0;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
       // Console.WriteLine("PeriodicTaskService is started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            //if (_taskStateService.IsRunning)
            //{
           // Console.WriteLine("PeriodicTaskService is Running.");
            await FetchAndProcessData();
            // Your periodic task logic here
            //  await Task.Delay(_interval);
            //}
            //else
            //{
            //    Console.WriteLine("PeriodicTaskService is stopped");
            //    //  await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken); // Delay to check _isRunning status again
            //    break;
            //}
        }
    }


    private async Task FetchAndProcessData()
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var fetcher = scope.ServiceProvider.GetRequiredService<HistoricalDataFetcher>();

        var startDate = DateTime.UtcNow.AddMinutes(-6000);
        //var startDate = DateTime.UtcNow.AddMinutes(-10000);
        var endDate = DateTime.UtcNow;

        var historicalData = await fetcher.FetchCandles("BTCUSD", "3m", startDate, endDate);

        var lastcandeltime = historicalData.FirstOrDefault()?.Time;
        var lastcandel = DateTimeOffset.FromUnixTimeSeconds(lastcandeltime ?? 0).UtcDateTime;
        var istTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
        var lastcandelT = TimeZoneInfo.ConvertTime(lastcandel, istTimeZone);
        _interval = -(DateTime.Now - TimeSpan.FromMinutes(6.40) - TimeZoneInfo.ConvertTime(lastcandel, istTimeZone));

        historicalData.Reverse();

        int shortTerm = 7;
        int longTerm = 21;

        //var movingAverages = MovingAverageAnalyzer.CalculateMovingAverages(historicalData, shortTerm, longTerm);
       // var angles = MovingAverageAnalyzer.CalculateAngles(movingAverages, shortTerm, longTerm);
        //var latestCrossover1 = MovingAverageAnalyzer.IdentifyCrossoversAndAngles(movingAverages, angles).LastOrDefault();


        // Load your historical candlestick data
        //var result = VolumeDryUpBacktest.RunBacktest(historicalData);

        //Console.WriteLine($"Total Trades: {result.TotalTrades}");
        //Console.WriteLine($"Winning Trades: {result.WinningTrades}");
        //Console.WriteLine($"Losing Trades: {result.LosingTrades}");
        //Console.WriteLine($"Total Profit: {result.TotalProfit}");
        //Console.WriteLine($"Max Drawdown: {result.MaxDrawdown}");


        int emaPeriod1 = 5; // You can change these values as per your requirement
        int emaPeriod2 = 10;

         var latestCrossoverEMA= EmaAnalyzer.IdentifyLatestCrossover(historicalData, emaPeriod1, emaPeriod2);


        int period = 3;
        VolumeMovingAverageCalculator calculator = new VolumeMovingAverageCalculator();
        List<decimal> volumeMovingAverages = calculator.CalculateVolumeMovingAverage(historicalData, period);

        //var tradeSignal = VolumeDryUpStrategy.GenerateTradeSignal(historicalData, 20);
        //if (tradeSignal != "No Signal") 
        //{
        //    var istDateTime = TimeZoneInfo.ConvertTime(DateTime.Now, istTimeZone);
        //    Log.Information($"Trade Signal: {tradeSignal} - {istDateTime}");
        //}
       

        //var result = EmaAnalyzer.IdentifyLatestCrossover(candles, shortTermEmaPeriod, longTermEmaPeriod);
        //Console.WriteLine($"Latest Crossover: {result.latestCrossoverType} at index {result.latestCrossoverIndex}");
        //Console.WriteLine($"EMA1 Angle: {result.latestEma1Angle}, EMA2 Angle: {result.latestEma2Angle}");
        //Console.WriteLine($"Latest Crossover: {result.latestCrossoverType} at index {result.latestCrossoverIndex}, " +
        //          $"EMA1 Angle: {result.latestEma1Angle}, EMA2 Angle: {result.latestEma2Angle}, " +
        //          $"Crossover occurred on candle: Time={result.latestCrossoverCandle.Time}, Open={result.latestCrossoverCandle.Open}, " +
        //          $"High={result.latestCrossoverCandle.High}, Low={result.latestCrossoverCandle.Low}, Close={result.latestCrossoverCandle.Close}, Volume={result.latestCrossoverCandle.Volume}");

        // if (result.IsCrossover)
        // {
        //     Log.Information(
        //$"Crossover: {result.IsCrossover}, " +
        //$"Type: {result.CrossoverType}, " +
        //$"EMA1 Angle: {result.Ema1Angle}, " +
        //$"EMA2 Angle: {result.Ema2Angle}, " +
        //$"Crossover Candle Open: {result.CrossoverCandleOpen}, " +
        //$"Crossover Candle Close: {result.CrossoverCandleClose}, " +
        //$"Crossover Candle High: {result.CrossoverCandleHigh}, " +
        //$"Crossover Candle Low: {result.CrossoverCandleLow}");

        //List<decimal> emaShort = new List<decimal> { 7 };
        //    List<decimal> emaLong = new List<decimal> { 21 };

        //    // Call the FindCrossoverPoint method
        //    decimal? crossoverPrice = EmaAnalyzer.FindCrossoverPoint(historicalData, emaShort, emaLong);

        //    // Output the crossover price if found
        //    if (crossoverPrice.HasValue)
        //    {
        //        Log.Information($"Crossover happened at price: {crossoverPrice.Value}");
        //    }
        // }

        List<string> timestamp = new List<string>();
       // if (latestCrossover != default)

            if (latestCrossoverEMA != default)
            {
            var isTrade = _appSettings.GetValue<bool>("Trade:IsTrade");

            //var istrade = true; //_taskStateService.IsTrade;
            var istDateTimenew = DateTime.UtcNow;

            //var istDateTimenew = TimeZoneInfo.ConvertTime(lastcandelT, istTimeZone);


            if (!timestamp.Contains(istDateTimenew.ToString("yyyy-MM-dd HH:mm:ss")))
            {
                var utcDateTime = DateTimeOffset.FromUnixTimeSeconds(latestCrossoverEMA.latestCrossoverCandle.Time).UtcDateTime;

                //var utcDateTime = DateTimeOffset.FromUnixTimeSeconds(latestCrossover.Timestamp).UtcDateTime;
                var istDateTime = TimeZoneInfo.ConvertTime(utcDateTime, istTimeZone);

                if (logTime != istDateTime.ToString("yyyy-MM-dd HH:mm:ss"))
                {
                    logTime = istDateTime.ToString("yyyy-MM-dd HH:mm:ss");
                    //Console.WriteLine("Latest Crossover: DateTime: {0}, Type: {1}, Angle: {2}, HIGH: {3}, LOW: {4}, OPEN: {5}, CLOSE: {6}, IsTrade: {7}",
                    //   istDateTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    //   latestCrossover.Type,
                    //   latestCrossover.Angle,
                    //   historicalData.FirstOrDefault()?.High ?? 0,
                    //   historicalData.FirstOrDefault()?.Low ?? 0,
                    //   historicalData.FirstOrDefault()?.Open ?? 0,
                    //   historicalData.FirstOrDefault()?.Close ?? 0,
                    //     isTrade);
                    //VolumeDivergence vd = new VolumeDivergence(2, 0.15m);
                    //bool isBullishDivergence = vd.IsBullishVolumeDivergence(historicalData);
                    //bool isBearishDivergence = vd.IsBearishVolumeDivergence(historicalData);

                    //Console.WriteLine("Bullish Divergence: " + isBullishDivergence);
                    //Console.WriteLine("Bearish Divergence: " + isBearishDivergence);

                    //int lookBackPeriod = 2;
                    //VolumeDivergenceDetector detector = new VolumeDivergenceDetector(lookBackPeriod);

                    //bool isBearishDivergence = detector.IsBullishVolumeDivergence(historicalData);
                    //bool isBullishDivergence = detector.IsBearishVolumeDivergence(historicalData);

                    bool isBearishDivergence = false;
                    bool isBullishDivergence = false;


                    var result = latestCrossoverEMA;
                    Log.Information($"Latest Crossover: {result.latestCrossoverType} at index {result.latestCrossoverIndex}, " +
                              $"EMA1 Angle: {result.latestEma1Angle}, EMA2 Angle: {result.latestEma2Angle}, " +
                              $"Crossover occurred on candle: Time={istDateTime.ToString("yyyy-MM-dd HH:mm:ss")}, Open={result.latestCrossoverCandle.Open}, " +
                              $"High={result.latestCrossoverCandle.High}, Low={result.latestCrossoverCandle.Low}, Close={result.latestCrossoverCandle.Close}, Volume={result.latestCrossoverCandle.Volume},"+
                              $"Bullish Divergence={isBullishDivergence}, Bearish Divergence={isBearishDivergence}");

                    var latestCrossover = new {Type = result.latestCrossoverType };

                    if (latestCrossover.Type == "Bullish")
                    {
                        isBullishDivergence = true;
                    }
                    else if (latestCrossover.Type == "Bearish")
                    {
                        isBearishDivergence = true;
                    }
                    //   Log.Information(
                    //$"Crossover: {latestCrossover.IsCrossover}, " +
                    //$"Type: {result.CrossoverType}, " +
                    //$"EMA1 Angle: {result.Ema1Angle}, " +
                    //$"EMA2 Angle: {result.Ema2Angle}, " +
                    //$"Crossover Candle Open: {result.CrossoverCandleOpen}, " +
                    //$"Crossover Candle Close: {result.CrossoverCandleClose}, " +
                    //$"Crossover Candle High: {result.CrossoverCandleHigh}, " +
                    //$"Crossover Candle Low: {result.CrossoverCandleLow}");

                    //          Log.Information("EMA Crossover Detected: {CrossoverType} Crossover. Short EMA Angle: {ShortEMAAngle}, Long EMA Angle: {LongEMAAngle}",
                    //result.CrossoverType ?? "No crossover", result.Ema1Angle, result.Ema2Angle);

                    // if (istrade && ((DateTime.Now - istDateTime).TotalMinutes < 5) && (!(isBullishDivergence && isBearishDivergence)))
                    Log.Information("DATETime: {UtcNow}, UtcDateTime: {UtcDateTime}, Difference (minutes): {Difference}",DateTime.UtcNow, utcDateTime,Math.Abs((DateTime.UtcNow - utcDateTime).TotalMinutes));
                    if (isTrade && (Math.Abs((DateTime.UtcNow - utcDateTime).TotalMinutes) < 5) && (!(isBullishDivergence && isBearishDivergence)))
                    {
                        //_logger.LogInformation("istrade is true. Time difference: {TimeDifference} minutes", (DateTime.Now - istDateTime).TotalMinutes);
                        Log.Information("istrade is true. Time difference: {TimeDifference} minutes", (DateTime.Now - istDateTime).TotalMinutes);
                        // if (Math.Abs(latestCrossover.Angle) >= 15)
                        {
                            //if (latestCrossover.Type == "Bullish")
                            if (latestCrossover.Type == "Bullish" && isBullishDivergence)
                            {
                               // _logger.LogInformation("Bullish crossover detected with bullish divergence.");
                                //await TradeAsync("buy");
                                try
                                {
                                    Log.Information("Bullish crossover detected with bullish divergence.");

                                    await Task.Run(async () =>
                                    {
                                        await TradeAsync("buy");
                                    });
                                }
                                catch (Exception ex)
                                {
                                    Log.Information($"Exception occurred: {ex.Message}");
                                    // Log or handle the exception
                                  //  Console.WriteLine($"Exception occurred: {ex.Message}");
                                }

                                // Add your trade logic here
                            }
                             else if (latestCrossover.Type == "Bearish" && isBearishDivergence)
                            //else if (latestCrossover.Type == "Bearish")
                            {
                                //
                                //await TradeAsync("sell");
                                try
                                {
                                    Log.Information("Bearish crossover detected with Bearish divergence.");
                                    await Task.Run(async () =>
                                    {
                                        await TradeAsync("sell");
                                    });
                                }
                                catch (Exception ex)
                                {
                                    Log.Information($"Exception occurred: {ex.Message}");
                                    // Log or handle the exception
                                   // Console.WriteLine($"Exception occurred: {ex.Message}");
                                }
                               // _logger.LogInformation("Bearish crossover detected with bearish divergence.");
                                // Add your trade logic here
                            }
                            else
                            {
                                //_logger.LogWarning("No valid trade detected. Crossover Type: {CrossoverType}, isBullishDivergence: {IsBullish}, isBearishDivergence: {IsBearish}", latestCrossover.Type, isBullishDivergence, isBearishDivergence);
                               // Console.WriteLine("NO trade");
                            }
                        }
                    }
                    else
                    {
                      //  _logger.LogWarning("istrade is false or conditions not met. Time difference: {TimeDifference} minutes", (DateTime.Now - istDateTime).TotalMinutes);
                    }
                }
                timestamp.Add(istDateTime.ToString("yyyy-MM-dd HH:mm:ss"));
                if (timestamp.Count() > 3)
                {
                    timestamp.RemoveAt(0);
                }
                 // Correct Console.WriteLine format
            }
        }

    }
    public async Task TradeAsync(string orderType)
    {
        // Initialize the DeltaAPI client with your API key and secret
        var deltaApi = new DeltaAPI("", "");

        // Define the symbol and quantity
        string symbol = "BTCUSD";
        decimal qty = 1;

        // Fetch the ticker information and place the order
        var (markPrice, productId) = await GetTickerAndProduct(symbol, deltaApi);
        var orderResponse = await PlaceOrder(productId, qty, markPrice,orderType, deltaApi);

        // Check the order status and handle it accordingly
       // await ManageOrder(orderResponse, productId, deltaApi);
    }
    // Private method to get ticker and product information
    private static async Task<(decimal, int)> GetTickerAndProduct(string symbol, DeltaAPI deltaApi)
    {
        JObject ticker = await deltaApi.GetTickerAsync(symbol);

        decimal markPrice = ticker["mark_price"].Value<decimal>();
        int productId = ticker["product_id"].Value<int>();
        Console.WriteLine($"Mark Price: {markPrice}, Product ID: {productId}");
        return (markPrice, productId);
    }

    // Private method to place a limit order
    private static async Task<JObject> PlaceOrder(int productId, decimal qty, decimal markPrice,string ordertype, DeltaAPI deltaApi)
    {
        JObject orderResponse = await deltaApi.PlaceOrderAsync(productId, qty, ordertype, markPrice);
        Console.WriteLine($"Order placed: {orderResponse}");
        return orderResponse;
    }

    // Private method to manage order status and cancellation
    private static async Task ManageOrder(JObject orderResponse, int productId, DeltaAPI deltaApi)
    {
        // Wait for a few seconds before checking the order status
        await Task.Delay(5000);

        // Check if the order is still open
        JArray liveOrders = await deltaApi.GetLiveOrdersAsync(productId);
        if (liveOrders.Count > 0)
        {
            foreach (var order in liveOrders)
            {
                if (order["id"].Value<string>() == orderResponse["id"].Value<string>())
                {
                    Console.WriteLine("Order is still open. Canceling...");
                    await deltaApi.CancelOrderAsync(order["id"].Value<string>(), productId);
                    Console.WriteLine("Order canceled.");
                    return;
                }
            }
        }
        else
        {
            Console.WriteLine("Order filled.");
        }
    }

}

