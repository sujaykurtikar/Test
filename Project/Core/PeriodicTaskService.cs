﻿using Newtonsoft.Json.Linq;
using Serilog;
using Timer = System.Threading.Timer;

public class PeriodicTaskService : BackgroundService
{
    //private readonly ILogger<PeriodicTaskService> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    //private bool _isRunning = true;
    //private CancellationTokenSource _cts;
   // private readonly ITaskStateService _taskStateService;
    private readonly IConfiguration _appSettings;
    private Timer _timer;

    public PeriodicTaskService(IServiceScopeFactory serviceScopeFactory, IConfiguration configuration)
    {
       // _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
       // _taskStateService = taskStateService;
        _appSettings = configuration;
    }

    private TimeSpan _interval = TimeSpan.FromMinutes(0);
    string logTime;
    string ImpulseMACDIndicator;
    //string trendML;
    string VolumeDryUp;
    string superTrend;
    //private long count = 0;
    private DateTime nextUpdateTime = DateTime.Now.AddMinutes(2);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
       // Console.WriteLine("PeriodicTaskService is started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            //if (_taskStateService.IsRunning)
            //{
            // Console.WriteLine("PeriodicTaskService is Running.");
            // _timer = new Timer(async _ => await FetchAndProcessData(), null, TimeSpan.Zero, _interval);
           // await Task.Delay(_interval);
            await FetchAndProcessData();
            // Your periodic task logic here
             // await Task.Delay(_interval);
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

        var resolution = _appSettings.GetValue<string>("Resolution");
        int value = int.Parse(new string(resolution.Where(char.IsDigit).ToArray()));
        var startDate = DateTime.UtcNow.AddMinutes(-(100* value)); // max 2000 candels 
        //var startDate = DateTime.UtcNow.AddMinutes(-10000);
        var endDate = DateTime.UtcNow;
        var historicalData = await fetcher.FetchCandles("BTCUSD", resolution, startDate, endDate);

        var lastcandeltime = historicalData.FirstOrDefault()?.Time;
        var lastcandel = DateTimeOffset.FromUnixTimeSeconds(lastcandeltime ?? 0).UtcDateTime;
        var istTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
        var lastcandelT = TimeZoneInfo.ConvertTime(lastcandel, istTimeZone);
        _interval = -(DateTime.UtcNow - TimeSpan.FromMinutes(6.40) - lastcandel);

        historicalData.Reverse();

        int shortTerm = _appSettings.GetValue<int>("Period:Period1");
        int longTerm = _appSettings.GetValue<int>("Period:Period2"); ;

        var movingAverages = MovingAverageAnalyzer.CalculateMovingAverages(historicalData, shortTerm, longTerm);
        var angles = MovingAverageAnalyzer.CalculateAngles(movingAverages, shortTerm, longTerm);
        var latestCrossover = MovingAverageAnalyzer.IdentifyCrossoversAndAngles(movingAverages, angles).LastOrDefault();

        var supertrend = new SupertrendIndicator(10, 3.0m);
        var supertrendSignals = supertrend.CalculateSupertrend(historicalData).LastOrDefault();
        if (supertrendSignals.Signal != superTrend)
        {
            superTrend = supertrendSignals.Signal;
            var istDateTime = TimeZoneInfo.ConvertTime(DateTime.UtcNow, istTimeZone);
            Log.Information($"Trade Signal Supertrend: {superTrend} -current time {istDateTime}");
        }

        var tradeSignal = VolumeDryUpStrategy.GenerateTradeSignal(historicalData, 20);
        if (tradeSignal != "No Signal" && tradeSignal != VolumeDryUp)
        {
            VolumeDryUp = tradeSignal;
            var istDateTime = TimeZoneInfo.ConvertTime(DateTime.UtcNow, istTimeZone);
            Log.Information($"Trade Signal VolumeDryUpStrategy: {tradeSignal} - {istDateTime}");
        }

        List<string> timestamp = new List<string>();
        if (latestCrossover != default)

          //  if (latestCrossoverEMA != default)
            {
            var isTrade = _appSettings.GetValue<bool>("Trade:IsTrade");

            //var istrade = true; //_taskStateService.IsTrade;
            var istDateTimenew = DateTime.UtcNow;

            //var istDateTimenew = TimeZoneInfo.ConvertTime(lastcandelT, istTimeZone);


            if (!timestamp.Contains(istDateTimenew.ToString("yyyy-MM-dd HH:mm:ss")))
            {
                //var utcDateTime = DateTimeOffset.FromUnixTimeSeconds(latestCrossoverEMA.Timestamp).UtcDateTime;

                var utcDateTime = DateTimeOffset.FromUnixTimeSeconds(latestCrossover.Timestamp).UtcDateTime;
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


                    //ImpulseMACDIndicator indicator = new ImpulseMACDIndicator();
                    //string latestSignal = indicator.GetLatestImpulseMACDSignal(historicalData);

                    ////bool isBearishDivergence = latestSignal == "Sell" ? true : false;
                    ////bool isBullishDivergence = latestSignal == "Buy" ? true : false;
                    //Log.Information($"The latest signal is MACDSignal: {latestSignal}");

                    bool isBearishDivergence = false;
                    bool isBullishDivergence = false ;
                    // var result = latestCrossoverEMA;
                    //Log.Information($"Latest Crossover: {result.latestCrossoverType} at index {result.latestCrossoverIndex}, " +
                    //$"EMA1 Angle: {result.latestEma1Angle}, EMA2 Angle: {result.latestEma2Angle}, " +
                    //$"Crossover occurred on candle: Time={istDateTime.ToString("yyyy-MM-dd HH:mm:ss")}, Open={result.latestCrossoverCandle.Open}, " +
                    //$"High={result.latestCrossoverCandle.High}, Low={result.latestCrossoverCandle.Low}, Close={result.latestCrossoverCandle.Close}, Volume={result.latestCrossoverCandle.Volume}," +
                    //$"Bullish Divergence={isBullishDivergence}, Bearish Divergence={isBearishDivergence}");

                    //Log.Information($"Latest Crossover: {result.CrossoverType} " +
                    //        $"Crossover Price: {result.CrossoverPrice}, " +
                    //        $"Crossover occurred on candle: Time={istDateTime:yyyy-MM-dd HH:mm:ss}, " +
                    //        $"Open={result.CrossoverCandle.Open}, High={result.CrossoverCandle.High}, Low={result.CrossoverCandle.Low}, " +
                    //        $"Close={result.CrossoverCandle.Close}, Volume={result.CrossoverCandle.Volume}, " +
                    //        $"Bullish Divergence={isBullishDivergence}, Bearish Divergence={isBearishDivergence}");
                    var crossoverCandle = historicalData.FirstOrDefault(c => c.Time == latestCrossover.Timestamp);

                    Log.Information($"current datetime now {TimeZoneInfo.ConvertTime(DateTime.UtcNow, istTimeZone)}, Crossover at Timestamp {istDateTime}, " +
                  $"Type: {latestCrossover.Type}, Angle: {latestCrossover.Angle}° " +
                  $"Candle Data - Open: {crossoverCandle.Open}, High: {crossoverCandle.High}, " +
                  $"Low: {crossoverCandle.Low}, Close: {crossoverCandle.Close}, Volume: {crossoverCandle.Volume}");

                    // var latestCrossover = new { Type = latestCrossoverEMA.Type };

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
                    if (isTrade && (Math.Abs((DateTime.UtcNow - utcDateTime).TotalMinutes) < value+5) && (!(isBullishDivergence && isBearishDivergence)))
                    {
                        //_logger.LogInformation("istrade is true. Time difference: {TimeDifference} minutes", (DateTime.Now - istDateTime).TotalMinutes);
                        Log.Information("istrade is true. Time difference: {TimeDifference} minutes", utcDateTime, Math.Abs((DateTime.UtcNow - utcDateTime).TotalMinutes));
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
                                    await Task.Delay(value);
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
                                    await Task.Delay(value);
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



                //if (DateTime.Now >= nextUpdateTime)
                //{
                //    candlestickModelManager.CheckAndUpdateModelAsync(historicalData);
                //    nextUpdateTime = DateTime.Now.AddHours(24); // Set the next update time to 24 hours from now
                //}
            }
        }
        scope.Dispose();
        historicalData = null;
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
        await PlaceOrder(productId, qty, markPrice, orderType, deltaApi);
       // var orderResponse = await PlaceOrder(productId, qty, markPrice,orderType, deltaApi);
        Log.Information($"Order placed: completed");
        // Check the order status and handle it accordingly
        // await ManageOrder(orderResponse, productId, deltaApi);
    }
    // Private method to get ticker and product information
    private static async Task<(decimal, int)> GetTickerAndProduct(string symbol, DeltaAPI deltaApi)
    {
        JObject ticker = await deltaApi.GetTickerAsync(symbol);

        decimal markPrice = ticker["mark_price"].Value<decimal>();
        int productId = ticker["product_id"].Value<int>();
        Log.Information($"Mark Price: {markPrice}, Product ID: {productId}");
        return (markPrice, productId);
    }

    // Private method to place a limit order
    private static async Task<JObject> PlaceOrder(int productId, decimal qty, decimal markPrice,string ordertype, DeltaAPI deltaApi)
    {
        JObject orderResponse = await deltaApi.PlaceOrderAsync(productId, qty, ordertype, markPrice);
        //Log.Information($"Order placed: {orderResponse}");
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

