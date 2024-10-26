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
    string priceAction;
    long? latestCandel;
    long? BBCandel;
    long? MA2Time; 
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
                    #region
                    //if (_taskStateService.IsRunning)
                    //{
                    // Console.WriteLine("PeriodicTaskService is Running.");
                    // _timer = new Timer(async _ => await FetchAndProcessData(), null, TimeSpan.Zero, _interval);
                    // await Task.Delay(_interval);

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
                    #endregion cmnt
                    await FetchAndProcessData();
                }
                catch (Exception ex)
                {
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
             _heartbeatTimer?.Dispose(); 

            var istTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            var dateTime = TimeZoneInfo.ConvertTime(DateTime.UtcNow, istTimeZone);
            Log.Information($"Background task stopped at {dateTime}");
        }
    }

    private async void HeartbeatCallback(object state)
    {
        var swaggerClient = new SwaggerClient();
        var swaggerPageContent = await swaggerClient.GetSwaggerPageAsync();

        if (swaggerPageContent != null)
        {
            Log.Information("Heartbeat signal sent to prevent idling.");
        }
        else
        {
            Log.Information("Failed to retrieve Swagger page content.");
        }

        #region
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
        #endregion cmnt trickr call
    }
    private async Task FetchAndProcessData()
    {
        try
        {
            var period = 20;
            using var scope = _serviceScopeFactory.CreateScope();
            var fetcher = scope.ServiceProvider.GetRequiredService<HistoricalDataFetcher>();

            var resolution = _appSettings.GetValue<string>("Resolution");
            int value = int.Parse(new string(resolution.Where(char.IsDigit).ToArray()));
            var startDate = DateTime.UtcNow.AddMinutes(-(100* value)); // max 2000 candels //var startDate = DateTime.UtcNow.AddMinutes(-10000);
            var endDate = DateTime.UtcNow;
            var historicalData = await fetcher.FetchCandles("BTCUSD", resolution, startDate, endDate);

            var lastCandelDetail = historicalData.FirstOrDefault();
            var lastcandeltime = lastCandelDetail?.Time;
            var lastcandel = DateTimeOffset.FromUnixTimeSeconds(lastcandeltime ?? 0).UtcDateTime;
            var istTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            var lastcandelT = TimeZoneInfo.ConvertTime(lastcandel, istTimeZone);
            //_interval = -(DateTime.UtcNow - TimeSpan.FromMinutes(6.40) - lastcandel);
            historicalData.Reverse();

            if (lastcandeltime != null && lastcandeltime != latestCandel)
            {
                Log.Information($"New candle fetched, Current time :{TimeZoneInfo.ConvertTime(DateTime.UtcNow, istTimeZone)}, New candel time: {lastcandelT}, CandelHigh:{lastCandelDetail.High} ,CandelLow: {lastCandelDetail.Low}, Candelopen:{lastCandelDetail.Open}, CandelLow:{lastCandelDetail.Close}");
                latestCandel = lastcandeltime;
            
            }

            var time = historicalData.LastOrDefault().Time;
            var strategy = new PriceActionStrategy(historicalData);
            var trend = strategy.GetTrendDirection(time);
            var (support, resistance) = strategy.GetSupportResistance(time, period);
            var breakout = strategy.IsBreakout(time, period);
            var pullback = strategy.IsPullback(time);
            var priceActionSignal = strategy.GetTradeSignalWithTimestamp(time, period);

            int shortTerm = _appSettings.GetValue<int>("Period:Period1");
            int longTerm = _appSettings.GetValue<int>("Period:Period2"); ;

            var movingAverages = MovingAverageAnalyzer.CalculateMovingAverages(historicalData, shortTerm, longTerm);
            var angles = MovingAverageAnalyzer.CalculateAngles(movingAverages, shortTerm, longTerm);
            var latestCrossover = MovingAverageAnalyzer.IdentifyCrossoversAndAngles(movingAverages, angles).LastOrDefault();

            List<string> timestamp = new List<string>();
            if (latestCrossover != default)
            {
                var isTrade = _appSettings.GetValue<bool>("Trade:IsTradeMA");

                var istDateTimenew = DateTime.UtcNow;

                if (!timestamp.Contains(istDateTimenew.ToString("yyyy-MM-dd HH:mm:ss")))
                {

                    var adxValues = ADXCalculator.CalculateADXForCandles(historicalData);
                    string adxTrend = ADXCalculator.CheckAdxReversal(adxValues);

                    var utcDateTime = DateTimeOffset.FromUnixTimeSeconds(latestCrossover.Timestamp).UtcDateTime;
                    var istDateTime = TimeZoneInfo.ConvertTime(utcDateTime, istTimeZone);

                    if (logTime != istDateTime.ToString("yyyy-MM-dd HH:mm:ss"))
                    {
                        logTime = istDateTime.ToString("yyyy-MM-dd HH:mm:ss");

                        bool isBearishDivergence = false;
                        bool isBullishDivergence = false;

                        //var adxValues = ADXCalculator.CalculateADXForCandles(historicalData);
                        //string adxTrend = ADXCalculator.CheckAdxReversal(adxValues);

                        var crossoverCandle = historicalData.Where(c => c.Time == latestCrossover.Timestamp).FirstOrDefault();

                        Log.Information($"current datetime now {TimeZoneInfo.ConvertTime(DateTime.UtcNow, istTimeZone)}, Crossover at Timestamp {istDateTime}, " +
                      $"Type: {latestCrossover.Type}, Angle: {latestCrossover.Angle}° " +
                      $"Candle Data - Open: {crossoverCandle.Open}, High: {crossoverCandle.High}, " +
                      $"Low: {crossoverCandle.Low}, Close: {crossoverCandle.Close}, Volume: {crossoverCandle.Volume}, AdXTrand: {adxTrend}, adxValues : {adxValues.LastOrDefault().ADX}");


                        if (latestCrossover.Type == "Bullish" && trend == "Uptrend"&&(adxTrend == "INC" || adxTrend == "REV"))
                        {
                            isBullishDivergence = true;
                        }
                        else if (latestCrossover.Type == "Bearish" && trend == "Downtrend"&&(adxTrend == "INC" || adxTrend == "REV"))
                        {
                            isBearishDivergence = true;
                        }

                        Log.Information("DATETime: {UtcNow}, UtcDateTime: {UtcDateTime}, Difference (minutes): {Difference}", DateTime.UtcNow, utcDateTime, Math.Abs((DateTime.UtcNow - utcDateTime).TotalMinutes));
                        if (isTrade && (Math.Abs((DateTime.UtcNow - utcDateTime).TotalMinutes) < value + 4) && (isBullishDivergence || isBearishDivergence))
                        {
                           // Log.Information("istrade is true. Time difference: {TimeDifference} minutes", utcDateTime, Math.Abs((DateTime.UtcNow - utcDateTime).TotalMinutes));

                            if (latestCrossover.Type == "Bullish" && isBullishDivergence)
                            {
                                try
                                {
                                    await TradeAsync("buy", crossoverCandle, "MA");
                                }
                                catch (Exception ex)
                                {
                                    Log.Information($"TradeAsync MA - Buy Exception occurred: {ex.Message}");
                                }
                            }
                            else if (latestCrossover.Type == "Bearish" && isBearishDivergence)
                            {
                                try
                                {
                                    await TradeAsync("sell", crossoverCandle, "MA");
                                }
                                catch (Exception ex)
                                {
                                    Log.Information($"TradeAsync MA - Sell Exception occurred: {ex.Message}");
                                }
                            }
                            else
                            {
                                //  Log.Information($"No Trade");
                            }
                        }
                        else 
                        {
                            //  _logger.LogWarning("istrade is false or conditions not met. Time difference: {TimeDifference} minutes", (DateTime.Now - istDateTime).TotalMinutes);
                        }
                    }
                    else if(adxTrend == "REV" && historicalData.Count >= 2 && latestCrossover.Timestamp == historicalData[historicalData.Count - 2].Time && MA2Time != latestCrossover.Timestamp) 
                    {
                        MA2Time = latestCrossover.Timestamp;
                        bool isBearishDivergence = false;
                        bool isBullishDivergence = false;
                        var isTrade2 = _appSettings.GetValue<bool>("Trade:IsTradeMA2");
                        //var adxValues = ADXCalculator.CalculateADXForCandles(historicalData);
                        //string adxTrend = ADXCalculator.CheckAdxReversal(adxValues);

                        var crossoverCandle = historicalData.Where(c => c.Time == latestCrossover.Timestamp).FirstOrDefault();

                        Log.Information($"current datetime now {TimeZoneInfo.ConvertTime(DateTime.UtcNow, istTimeZone)}, Crossover at Timestamp {istDateTime}, " +
                      $"Type: {latestCrossover.Type}, Angle: {latestCrossover.Angle}° " +
                      $"Candle Data - Open: {crossoverCandle.Open}, High: {crossoverCandle.High}, " +
                      $"Low: {crossoverCandle.Low}, Close: {crossoverCandle.Close}, Volume: {crossoverCandle.Volume}, AdXTrand: {adxTrend}, adxValues : {adxValues.LastOrDefault().ADX}");


                        if (latestCrossover.Type == "Bullish" && trend == "Uptrend" && (adxTrend == "INC" || adxTrend == "REV"))
                        {
                            isBullishDivergence = true;
                        }
                        else if (latestCrossover.Type == "Bearish" && trend == "Downtrend" && (adxTrend == "INC" || adxTrend == "REV"))
                        {
                            isBearishDivergence = true;
                        }

                        Log.Information("DATETime: {UtcNow}, UtcDateTime: {UtcDateTime}, Difference (minutes): {Difference}", DateTime.UtcNow, utcDateTime, Math.Abs((DateTime.UtcNow - utcDateTime).TotalMinutes));
                        if (isTrade2 && (isBullishDivergence || isBearishDivergence))
                        {
                            //Log.Information("istrade is true. Time difference: {TimeDifference} minutes", utcDateTime, Math.Abs((DateTime.UtcNow - utcDateTime).TotalMinutes));

                            if (latestCrossover.Type == "Bullish" && isBullishDivergence)
                            {
                                try
                                {
                                    await TradeAsync("buy", crossoverCandle, "MA");
                                }
                                catch (Exception ex)
                                {
                                    Log.Information($"TradeAsync MA - Buy Exception occurred: {ex.Message}");
                                }
                            }
                            else if (latestCrossover.Type == "Bearish" && isBearishDivergence)
                            {
                                try
                                {
                                    await TradeAsync("sell", crossoverCandle, "MA");
                                }
                                catch (Exception ex)
                                {
                                    Log.Information($"TradeAsync MA - Sell Exception occurred: {ex.Message}");
                                }
                            }
                            else
                            {
                                //  Log.Information($"No Trade");
                            }
                        }

                    }

                    timestamp.Add(istDateTime.ToString("yyyy-MM-dd HH:mm:ss"));
                    if (timestamp.Count() > 3)
                    {
                        timestamp.RemoveAt(0);
                    }
                }
            }

            var calculator = new BollingerBandCalculator();

            var signals = calculator.GetLatestSignal(historicalData);

            var istTimeZone1 = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            var dateTime = TimeZoneInfo.ConvertTime(DateTime.UtcNow, istTimeZone1);

            var breakoutDetector = new BreakoutDetector(historicalData);
            //string result = breakoutDetector.CheckBreakout(historicalData.LastOrDefault());
            //Log.Information($"breakoutDetector :{result}");

            var SignalTime = DateTimeOffset.FromUnixTimeSeconds(signals?.Time ?? 0).UtcDateTime;
            var SignaldateTime = TimeZoneInfo.ConvertTime(SignalTime, istTimeZone1);
            bollbingerBand = signals.SignalType.ToString();

            var bollbingerTrand = bollbingerBand == "Buy" ? "Uptrend" : "Downtrend";

            if (trend == bollbingerTrand && latestCandel == signals?.Time && BBCandel != signals?.Time)
            {
                BBCandel = signals?.Time;
                Log.Information($"BollingerBandCalculator: {bollbingerBand}, candelTime {SignaldateTime}, current time {dateTime}");
                var isTrade = _appSettings.GetValue<bool>("Trade:IsTradeBB");
                var SignaldateTime2 = TimeZoneInfo.ConvertTime(DateTimeOffset.FromUnixTimeSeconds(priceActionSignal?.Timestamp ?? 0).UtcDateTime, istTimeZone);

                priceAction = priceActionSignal.Signal;

                Log.Information(" Price Action Trend: {Trend}; Support: {Support}; Resistance: {Resistance}; Breakout: {Breakout}; Pullback: {Pullback}; Trade Signal: {TradeSignal}; Timestamp: {Timestamp}",
                     trend, support, resistance, breakout, pullback, priceAction, SignaldateTime2);
                if (isTrade)
                {
                    if (trend == "Uptrend")
                    {
                        try
                        {
                            await TradeAsync("buy", lastCandelDetail, "BB");
                        }
                        catch (Exception ex)
                        {
                            Log.Information($"TradeAsync BB - Buy Exception occurred: {ex.Message}");
                        }
                    }
                    else if (trend == "Downtrend")
                    {
                        try
                        {
                            await TradeAsync("sell", lastCandelDetail, "BB");
                        }
                        catch (Exception ex)
                        {
                            Log.Information($"TradeAsync BB - Sell Exception occurred: {ex.Message}");
                        }
                    }
                }
            }
            //  }

            #region
            //var time = historicalData.LastOrDefault().Time;
            //var strategy = new PriceActionStrategy(historicalData);
            //var trend = strategy.GetTrendDirection(time);
            //var (support, resistance) = strategy.GetSupportResistance(time, period);
            //var breakout = strategy.IsBreakout(time, period);
            //var pullback = strategy.IsPullback(time);
            //var priceActionSignal = strategy.GetTradeSignalWithTimestamp(time, period);

            //if (priceActionSignal != null && priceActionSignal.Signal != priceAction)
            //{
            //    var istTimeZone1 = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            //    var dateTime = TimeZoneInfo.ConvertTime(DateTime.UtcNow, istTimeZone1);

            //    var SignalTime = DateTimeOffset.FromUnixTimeSeconds(priceActionSignal?.Timestamp ?? 0).UtcDateTime;
            //    var SignaldateTime = TimeZoneInfo.ConvertTime(SignalTime, istTimeZone1);
            //    priceAction = priceActionSignal.Signal;

            //    Log.Information(" Price Action Trend: {Trend}; Support: {Support}; Resistance: {Resistance}; Breakout: {Breakout}; Pullback: {Pullback}; Trade Signal: {TradeSignal}; Timestamp: {Timestamp}",
            //         trend, support, resistance, breakout, pullback, priceAction, SignaldateTime);
            //}
            #endregion priceAction Cmnt

            #region
            //int shortTerm = _appSettings.GetValue<int>("Period:Period1");
            //int longTerm = _appSettings.GetValue<int>("Period:Period2"); ;

            //var movingAverages = MovingAverageAnalyzer.CalculateMovingAverages(historicalData, shortTerm, longTerm);
            //var angles = MovingAverageAnalyzer.CalculateAngles(movingAverages, shortTerm, longTerm);
            //var latestCrossover = MovingAverageAnalyzer.IdentifyCrossoversAndAngles(movingAverages, angles).LastOrDefault();

            //#region
            ////var supertrend = new SupertrendIndicator(10, 3.0m);
            ////var supertrendSignals = supertrend.CalculateSupertrend(historicalData).LastOrDefault();
            ////if (supertrendSignals.Signal != superTrend)
            ////{
            ////    superTrend = supertrendSignals.Signal;
            ////    var istDateTime = TimeZoneInfo.ConvertTime(DateTime.UtcNow, istTimeZone);
            ////    Log.Information($"Trade Signal Supertrend: {superTrend} -current time {istDateTime}");
            ////}


            ////var tradeSignal = VolumeDryUpStrategy.GenerateTradeSignal(historicalData, 20);
            ////if (tradeSignal != "No Signal" && tradeSignal != VolumeDryUp)
            ////{
            ////    VolumeDryUp = tradeSignal;
            ////    var istDateTime = TimeZoneInfo.ConvertTime(DateTime.UtcNow, istTimeZone);
            ////    Log.Information($"Trade Signal VolumeDryUpStrategy: {tradeSignal} - {istDateTime}");
            ////}
            //#endregion cmnt
            //List<string> timestamp = new List<string>();
            //if (latestCrossover != default)
            //{
            //    var isTrade = _appSettings.GetValue<bool>("Trade:IsTrade");

            //    var istDateTimenew = DateTime.UtcNow;

            //    if (!timestamp.Contains(istDateTimenew.ToString("yyyy-MM-dd HH:mm:ss")))
            //    {

            //        var utcDateTime = DateTimeOffset.FromUnixTimeSeconds(latestCrossover.Timestamp).UtcDateTime;
            //        var istDateTime = TimeZoneInfo.ConvertTime(utcDateTime, istTimeZone);

            //        if (logTime != istDateTime.ToString("yyyy-MM-dd HH:mm:ss"))
            //        {
            //            logTime = istDateTime.ToString("yyyy-MM-dd HH:mm:ss");

            //            bool isBearishDivergence = false;
            //            bool isBullishDivergence = false;

            //            var adxValues = ADXCalculator.CalculateADXForCandles(historicalData);
            //            string adxTrend = ADXCalculator.CheckAdxReversal(adxValues);

            //            var crossoverCandle = historicalData.Where(c => c.Time == latestCrossover.Timestamp).FirstOrDefault();

            //            Log.Information($"current datetime now {TimeZoneInfo.ConvertTime(DateTime.UtcNow, istTimeZone)}, Crossover at Timestamp {istDateTime}, " +
            //          $"Type: {latestCrossover.Type}, Angle: {latestCrossover.Angle}° " +
            //          $"Candle Data - Open: {crossoverCandle.Open}, High: {crossoverCandle.High}, " +
            //          $"Low: {crossoverCandle.Low}, Close: {crossoverCandle.Close}, Volume: {crossoverCandle.Volume}, AdXTrand: {adxTrend}, adxValues : {adxValues.LastOrDefault().ADX}");


            //            if (latestCrossover.Type == "Bullish" && (adxTrend == "INC" || adxTrend == "REV"))
            //            {
            //                isBullishDivergence = true;
            //            }
            //            else if (latestCrossover.Type == "Bearish" && (adxTrend == "INC" || adxTrend == "REV"))
            //            {
            //                isBearishDivergence = true;
            //            }

            //            Log.Information("DATETime: {UtcNow}, UtcDateTime: {UtcDateTime}, Difference (minutes): {Difference}", DateTime.UtcNow, utcDateTime, Math.Abs((DateTime.UtcNow - utcDateTime).TotalMinutes));
            //            if (isTrade && (Math.Abs((DateTime.UtcNow - utcDateTime).TotalMinutes) < value + 5) && (isBullishDivergence || isBearishDivergence))
            //            {
            //                Log.Information("istrade is true. Time difference: {TimeDifference} minutes", utcDateTime, Math.Abs((DateTime.UtcNow - utcDateTime).TotalMinutes));

            //                if (latestCrossover.Type == "Bullish" && isBullishDivergence)
            //                {
            //                    try
            //                    {
            //                        await TradeAsync("buy", crossoverCandle, "MA");
            //                    }
            //                    catch (Exception ex)
            //                    {
            //                        Log.Information($"TradeAsync - Buy Exception occurred: {ex.Message}");
            //                    }
            //                }
            //                else if (latestCrossover.Type == "Bearish" && isBearishDivergence)
            //                {
            //                    try
            //                    {
            //                        await TradeAsync("sell", crossoverCandle, "MA");
            //                    }
            //                    catch (Exception ex)
            //                    {
            //                        Log.Information($"TradeAsync - Sell Exception occurred: {ex.Message}");
            //                    }
            //                }
            //                else
            //                {
            //                  //  Log.Information($"No Trade");
            //                }
            //            }
            //            else
            //            {
            //                //  _logger.LogWarning("istrade is false or conditions not met. Time difference: {TimeDifference} minutes", (DateTime.Now - istDateTime).TotalMinutes);
            //            }
            //        }
            //        timestamp.Add(istDateTime.ToString("yyyy-MM-dd HH:mm:ss"));
            //        if (timestamp.Count() > 3)
            //        {
            //            timestamp.RemoveAt(0);
            //        }
            //    }
            //}
            #endregion Old code of CrossOver
            historicalData = null;
        }
        catch (Exception ex)
        {
            Log.Information($"Exception occurred At candel Fetch: {ex.Message}");

        }
    }
    public async Task TradeAsync(string orderType, Candlestick candle, string type)
    {
        // Initialize the DeltaAPI client with your API key and secret
        var deltaApi = new DeltaAPI("", "", _appSettings);

        // Define the symbol and quantity
        string symbol = "BTCUSD";
        decimal qty = 1;

        // Fetch the ticker information and place the order
        var (markPrice, productId) = await GetTickerAndProduct(symbol, deltaApi);
        await PlaceOrder(productId, qty, markPrice, orderType, deltaApi, candle, type);
       // var orderResponse = await PlaceOrder(productId, qty, markPrice,orderType, deltaApi);
        Log.Information($"Order Process completed");
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
    private static async Task<JObject> PlaceOrder(int productId, decimal qty, decimal markPrice,string ordertype, DeltaAPI deltaApi, Candlestick candle, string type)
    {
        JObject orderResponse = await deltaApi.PlaceOrderAsync(productId, qty, ordertype, markPrice, candle, type);
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

