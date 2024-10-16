using Newtonsoft.Json.Linq;
using Serilog;
using System.Reflection.Metadata;
using static HaramiTradingStrategy;
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
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(1);
    private Timer _heartbeatTimer;
    public PeriodicTaskService(IServiceScopeFactory serviceScopeFactory, IConfiguration configuration)
    {
       // _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
       // _taskStateService = taskStateService;
        _appSettings = configuration;
    }

    //private TimeSpan _interval = TimeSpan.FromMinutes(0);
    string logTime;
    string ImpulseMACDIndicator;
    //string trendML;
    string VolumeDryUp;
    string superTrend;
    string bollbingerBand;
    long? latestCandel;
    //private long count = 0;
    private DateTime nextUpdateTime = DateTime.Now.AddMinutes(2);
    

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
       // Console.WriteLine("PeriodicTaskService is started.");
        try
        {
            var istTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            var dateTime = TimeZoneInfo.ConvertTime(DateTime.UtcNow, istTimeZone);
            Log.Information($"Background task started {dateTime}");

            _heartbeatTimer = new Timer(HeartbeatCallback, null, TimeSpan.Zero, TimeSpan.FromMinutes(10));
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    //if (_taskStateService.IsRunning)
                    //{
                    // Console.WriteLine("PeriodicTaskService is Running.");
                    // _timer = new Timer(async _ => await FetchAndProcessData(), null, TimeSpan.Zero, _interval);
                    // await Task.Delay(_interval);
                    await FetchAndProcessData();
                    // await Task.Delay(_interval, stoppingToken);
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
                catch (Exception ex)
                {
                    // Log the error without stopping the task
                    Log.Error(ex, "Error occurred while processing data while await FetchAndProcessData()");
                }
            }

        }
        catch (Exception ex)
        {
            var istTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            var dateTime = TimeZoneInfo.ConvertTime(DateTime.UtcNow, istTimeZone);
            Log.Information(ex, $"Background task encountered an error at {dateTime}");
        }
        finally
        {
          
             _heartbeatTimer?.Dispose(); // Only dispose if stopping

            var istTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            var dateTime = TimeZoneInfo.ConvertTime(DateTime.UtcNow, istTimeZone);
            Log.Information($"Background task stopped at {dateTime}");
        }
    }

    private async void HeartbeatCallback(object state)
    {
        // This method will be called by the timer at regular intervals
        // Log.Information("Heartbeat signal sent to prevent idling.");

        var swaggerClient = new SwaggerClient();
        var swaggerPageContent = await swaggerClient.GetSwaggerPageAsync();

        if (swaggerPageContent != null)
        {
            // Display the Swagger page content (HTML)
           // Console.WriteLine(swaggerPageContent);

            Log.Information("Heartbeat signal sent to prevent idling.");
        }
        else
        {
            Log.Information("Failed to retrieve Swagger page content.");
        }

        //var url = "http://testtrading.somee.com/publish/Ticker/BTCUSD%20";

        //using (var client = new HttpClient())
        //{
        //    // Set the Accept header
        //    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("*/*"));

        //    try
        //    {
        //        // Make the GET request
        //        var response = await client.GetAsync(url);

        //        // Check if the response is successful
        //        response.EnsureSuccessStatusCode();

        //        // Read the response content
        //        var content = await response.Content.ReadAsStringAsync();

        //        // Output the response content
        //        // Console.WriteLine(content);

        //        Log.Information("Heartbeat signal sent to prevent idling.");
        //    }
        //    catch (HttpRequestException e)
        //    {
        //        // Handle exceptions
        //        Log.Information($"Request error: {e.Message}");
        //    }
        //}
        // Optionally, perform a lightweight operation here if needed
    }
    private async Task FetchAndProcessData()
    {
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var fetcher = scope.ServiceProvider.GetRequiredService<HistoricalDataFetcher>();

            var resolution = _appSettings.GetValue<string>("Resolution");
            int value = int.Parse(new string(resolution.Where(char.IsDigit).ToArray()));
            var startDate = DateTime.UtcNow.AddMinutes(-(100* value)); // max 2000 candels 
                                                                        //var startDate = DateTime.UtcNow.AddMinutes(-10000);
            var endDate = DateTime.UtcNow;
            var historicalData = await fetcher.FetchCandles("BTCUSD", resolution, startDate, endDate);

            var lastCandelDetail = historicalData.FirstOrDefault();
            var lastcandeltime = lastCandelDetail?.Time;
            var lastcandel = DateTimeOffset.FromUnixTimeSeconds(lastcandeltime ?? 0).UtcDateTime;
            var istTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            var lastcandelT = TimeZoneInfo.ConvertTime(lastcandel, istTimeZone);
            //_interval = -(DateTime.UtcNow - TimeSpan.FromMinutes(6.40) - lastcandel);

            if (lastcandeltime != null && lastcandeltime != latestCandel)
            {
                Log.Information($"New candle fetched, Current time :{TimeZoneInfo.ConvertTime(DateTime.UtcNow, istTimeZone)}, New candel time: {lastcandelT}, CandelHigh:{lastCandelDetail.High} ,CandelLow: {lastCandelDetail.Low}, Candelopen:{lastCandelDetail.Open}, CandelLow:{lastCandelDetail.Close}");
                latestCandel = lastcandeltime;

                var calculator = new BollingerBandCalculator();
                var candels = historicalData;
                candels.Reverse();
                var signals = calculator.GenerateSignals(candels).LastOrDefault();

                if (signals != null && signals.SignalType.ToString() != bollbingerBand)
                {
                    var istTimeZone1 = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
                    var dateTime = TimeZoneInfo.ConvertTime(DateTime.UtcNow, istTimeZone1);

                    var SignalTime = DateTimeOffset.FromUnixTimeSeconds(signals?.Time ?? 0).UtcDateTime;
                    var SignaldateTime = TimeZoneInfo.ConvertTime(SignalTime, istTimeZone1);
                    bollbingerBand = signals.SignalType.ToString();
                    Log.Information($"BollingerBandCalculator: {bollbingerBand}, candelTime {SignaldateTime}, current time {dateTime}");
                }          

            }

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
                        bool isBullishDivergence = false;
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

                        var adxValues = ADXCalculator.CalculateADXForCandles(historicalData);
                        string adxTrend = ADXCalculator.CheckAdxReversal(adxValues);

                        //foreach (var (Time, ADX) in adxValues)
                        //{
                        //    var utcDateTime1 = DateTimeOffset.FromUnixTimeSeconds(Time).UtcDateTime;
                        //    var istDateTime1 = TimeZoneInfo.ConvertTime(utcDateTime1, istTimeZone);
                        //    Console.WriteLine($"Timestamp: {istDateTime1}, ADX: {ADX}");
                        //}

                        var crossoverCandle = historicalData.Where(c => c.Time == latestCrossover.Timestamp).FirstOrDefault();

                        Log.Information($"current datetime now {TimeZoneInfo.ConvertTime(DateTime.UtcNow, istTimeZone)}, Crossover at Timestamp {istDateTime}, " +
                      $"Type: {latestCrossover.Type}, Angle: {latestCrossover.Angle}° " +
                      $"Candle Data - Open: {crossoverCandle.Open}, High: {crossoverCandle.High}, " +
                      $"Low: {crossoverCandle.Low}, Close: {crossoverCandle.Close}, Volume: {crossoverCandle.Volume}, AdXTrand: {adxTrend}, adxValues : {adxValues.LastOrDefault().ADX}");


                        if (latestCrossover.Type == "Bullish" && (adxTrend == "INC" || adxTrend == "REV"))
                        {
                            isBullishDivergence = true;
                        }
                        else if (latestCrossover.Type == "Bearish" && (adxTrend == "INC" || adxTrend == "REV"))
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
                        Log.Information("DATETime: {UtcNow}, UtcDateTime: {UtcDateTime}, Difference (minutes): {Difference}", DateTime.UtcNow, utcDateTime, Math.Abs((DateTime.UtcNow - utcDateTime).TotalMinutes));
                        if (isTrade && (Math.Abs((DateTime.UtcNow - utcDateTime).TotalMinutes) < value + 5) && (isBullishDivergence || isBearishDivergence))
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

                                        //  await Task.Run(async () =>
                                        // {
                                        await TradeAsync("buy");
                                        // });
                                        //await Task.Delay(value);
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
                                        // await Task.Run(async () =>
                                        // {
                                        await TradeAsync("sell");
                                        // });
                                        // await Task.Delay(value);
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
            historicalData = null;
        }
        catch (Exception ex)
        {
            Log.Information($"Exception occurred At candel Fetch: {ex.Message}");
          
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

