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

public class PeriodicTaskService : BackgroundService
{
    private readonly ILogger<PeriodicTaskService> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    //private bool _isRunning = true;
    //private CancellationTokenSource _cts;
    private readonly ITaskStateService _taskStateService;

    public PeriodicTaskService(ILogger<PeriodicTaskService> logger, IServiceScopeFactory serviceScopeFactory, ITaskStateService taskStateService)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
        _taskStateService = taskStateService;
    }

    private TimeSpan _interval = TimeSpan.FromMinutes(5);
    private bool _isRunning = false;
    private readonly object _lock = new object();
    string logTime;
    private long count = 0;
    //public void Start()
    //{
    //    lock (_lock)
    //    {
    //        Console.WriteLine("PeriodicTaskService is _isRunning = true.");
    //        _taskStateService.IsRunning = true;
    //        _isRunning = true;
    //    }
    //}

    //public void Stop()
    //{
    //    lock (_lock)
    //    {
    //        _taskStateService.IsRunning = false;
    //        _cts.Cancel();
    //    }
    //}

    //public bool IsRunning
    //{
    //    get
    //    {
    //        lock (_lock)
    //        {
    //            return _isRunning;
    //        }
    //    }
    //}

    //public PeriodicTaskService(ILogger<PeriodicTaskService> logger, IServiceScopeFactory serviceScopeFactory, ITaskStateService taskStateService)
    //{
    //    _logger = logger;
    //    _serviceScopeFactory = serviceScopeFactory;
    //    _taskStateService = taskStateService;
    //}

    //public void Start()
    //{

    //    _logger.LogInformation("Starting PeriodicTaskService.");
    //    // Cancel any existing tasks
    //    _taskStateService.IsTrade = true;
    //}

    //public void Stop()
    //{
    //    _logger.LogInformation("Stopping PeriodicTaskService.");
    //    _taskStateService.IsTrade = false;
    //    // Cancel ongoing tasks
    //}

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

        var startDate = DateTime.UtcNow.AddMinutes(-10000);
        var endDate = DateTime.UtcNow;

        var historicalData = await fetcher.FetchCandles("BTCUSD", "5m", startDate, endDate);

        var lastcandeltime = historicalData.FirstOrDefault()?.Time;
        var lastcandel = DateTimeOffset.FromUnixTimeSeconds(lastcandeltime ?? 0).UtcDateTime;
        var istTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
        var lastcandelT = TimeZoneInfo.ConvertTime(lastcandel, istTimeZone);
        _interval = -(DateTime.Now - TimeSpan.FromMinutes(6.40) - TimeZoneInfo.ConvertTime(lastcandel, istTimeZone));

        historicalData.Reverse();

        int shortTerm = 5;
        int longTerm = 15;

        var movingAverages = MovingAverageAnalyzer.CalculateMovingAverages(historicalData, shortTerm, longTerm);
        var angles = MovingAverageAnalyzer.CalculateAngles(movingAverages, shortTerm, longTerm);
        var latestCrossover = MovingAverageAnalyzer.IdentifyCrossoversAndAngles(movingAverages, angles).LastOrDefault();
        List<string> timestamp = new List<string>();
        if (latestCrossover != default)
        {
            var istrade = true; //_taskStateService.IsTrade;
           var istDateTimenew = TimeZoneInfo.ConvertTime(lastcandelT, istTimeZone);

            if (!timestamp.Contains(istDateTimenew.ToString("yyyy-MM-dd HH:mm:ss")))
            {
                var utcDateTime = DateTimeOffset.FromUnixTimeSeconds(latestCrossover.Timestamp).UtcDateTime;
                var istDateTime = TimeZoneInfo.ConvertTime(utcDateTime, istTimeZone);

                if (logTime != istDateTime.ToString("yyyy-MM-dd HH:mm:ss"))
                {
                    logTime = istDateTime.ToString("yyyy-MM-dd HH:mm:ss");
                    Console.WriteLine("Latest Crossover: DateTime: {0}, Type: {1}, Angle: {2}, HIGH: {3}, LOW: {4}, OPEN: {5}, CLOSE: {6}, IsTrade: {7}",
                       istDateTime.ToString("yyyy-MM-dd HH:mm:ss"),
                       latestCrossover.Type,
                       latestCrossover.Angle,
                       historicalData.FirstOrDefault()?.High ?? 0,
                       historicalData.FirstOrDefault()?.Low ?? 0,
                       historicalData.FirstOrDefault()?.Open ?? 0,
                       historicalData.FirstOrDefault()?.Close ?? 0,
                         istrade);
                    VolumeDivergence vd = new VolumeDivergence(3, 0.15m);
                    bool isBullishDivergence = vd.IsBullishVolumeDivergence(historicalData);
                    bool isBearishDivergence = vd.IsBearishVolumeDivergence(historicalData);

                    Console.WriteLine("Bullish Divergence: " + isBullishDivergence);
                    Console.WriteLine("Bearish Divergence: " + isBearishDivergence);

                    if (istrade && ((DateTime.Now - istDateTime).TotalMinutes < 5) && (!(isBullishDivergence && isBearishDivergence)))
                    {
                        _logger.LogInformation("istrade is true. Time difference: {TimeDifference} minutes", (DateTime.Now - istDateTime).TotalMinutes);

                        if (Math.Abs(latestCrossover.Angle) >= 40)
                        {
                            if (latestCrossover.Type == "Bullish")
                            //if (latestCrossover.Type == "Bullish" && isBullishDivergence)
                            {
                                _logger.LogInformation("Bullish crossover detected with bullish divergence.");
                                //await TradeAsync("buy");
                                try
                                {
                                    await Task.Run(async () =>
                                    {
                                        await TradeAsync("buy");
                                    });
                                }
                                catch (Exception ex)
                                {
                                    // Log or handle the exception
                                    Console.WriteLine($"Exception occurred: {ex.Message}");
                                }

                                // Add your trade logic here
                            }
                            // else if (latestCrossover.Type == "Bearish" && isBearishDivergence)
                            else if (latestCrossover.Type == "Bearish")
                            {
                                //
                                //await TradeAsync("sell");
                                try
                                {
                                    await Task.Run(async () =>
                                    {
                                        await TradeAsync("sell");
                                    });
                                }
                                catch (Exception ex)
                                {
                                    // Log or handle the exception
                                    Console.WriteLine($"Exception occurred: {ex.Message}");
                                }
                                _logger.LogInformation("Bearish crossover detected with bearish divergence.");
                                // Add your trade logic here
                            }
                            else
                            {
                                _logger.LogWarning("No valid trade detected. Crossover Type: {CrossoverType}, isBullishDivergence: {IsBullish}, isBearishDivergence: {IsBearish}", latestCrossover.Type, isBullishDivergence, isBearishDivergence);
                                Console.WriteLine("NO trade");
                            }
                        }
                    }
                    else
                    {
                      //  _logger.LogWarning("istrade is false or conditions not met. Time difference: {TimeDifference} minutes", (DateTime.Now - istDateTime).TotalMinutes);
                    }
                }
                timestamp.Add(istDateTime.ToString("yyyy-MM-dd HH:mm:ss"));
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

