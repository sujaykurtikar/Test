using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileSystemGlobbing;
using Serilog;
using static Test_Project.Controllers.ControlController;

namespace Test_Project.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ControlController : ControllerBase
    {
      
        private readonly PeriodicTaskService _periodicTaskService;
        //// Constructor with dependency injection
        //public ControlController(ControlService controlService, PeriodicTaskService periodicTaskService)
        //{
        //    _controlService = controlService ?? throw new ArgumentNullException(nameof(controlService));
        //    _periodicTaskService = periodicTaskService ?? throw new ArgumentNullException(nameof(periodicTaskService));
        //}

        //[HttpPost("start")]
        //public IActionResult StartService()
        //{
        //    _controlService.StartService();
        //    return Ok("Service started.");
        //}

        //[HttpPost("stop")]
        //public IActionResult StopService()
        //{
        //    _controlService.StopService();
        //    return Ok("Service stopped.");
        //}

        //[HttpGet("status")]
        //public IActionResult GetStatus()
        //{
        //    //var status = _periodicTaskService.IsRunning ? "running" : "stopped";
        //   // return Ok(new { Status = status });
        //}

        public interface ITaskStateService
        {
            bool IsTrade{ get; set; }
        }
        public class TaskStateService : ITaskStateService
        {
            private bool _isTrade;

            public bool IsTrade
            {
                get => _isTrade;
                set => _isTrade = value;
            }
        }
        //private readonly ITaskStateService _taskStateService;

        //public ControlController(ITaskStateService taskStateService)
        //{
        //    _taskStateService = taskStateService;
        //}

        //[HttpPost("start")]
        //public IActionResult Start()
        //{
        //    _taskStateService.IsRunning = true;
        //    return Ok("Service started.");
        //}

        //[HttpPost("stop")]
        //public IActionResult Stop()
        //{
        //    _taskStateService.IsRunning = false;
        //    return Ok("Service stopped.");
        //}

        private readonly ITaskStateService _taskStateService;
       // private readonly PeriodicTaskService _periodicTaskService;

        public ControlController(ITaskStateService taskStateService, PeriodicTaskService periodicTaskService)
        {
            
            _taskStateService = taskStateService;
            _periodicTaskService = periodicTaskService;
        }

        [HttpPost("start")]
        public IActionResult Start()
        {

            // _periodicTaskService.Start();
            // Ensure the service starts
            // Log.Information("Service started at {DateTime.Now}.");

            TimeZoneInfo localTimeZone = TimeZoneInfo.Local;
            DateTime utcDateTime = DateTime.UtcNow;

            TimeZoneInfo istTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");

            DateTime istDateTime = TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, istTimeZone);

            return Ok(new
            {
                LocalTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                LocalTimeZone = localTimeZone.DisplayName,
                UtcTime = utcDateTime.ToString("yyyy-MM-dd HH:mm:ss"),
                IstTime = istDateTime.ToString("yyyy-MM-dd HH:mm:ss")
            });
        }

        [HttpPost("stop")]
        public IActionResult Stop()
        {
            if (_taskStateService.IsTrade)
            {
                //_taskStateService.IsTrade = false;
                //  _periodicTaskService.Stop(); // Ensure the service stops
                return Ok($"Service started at DateTime.UtcNow {DateTime.UtcNow}.");
            }
            return BadRequest("Service is not running.");
        }

        [HttpPost("BollingerBandSignal")]
        public async Task<ActionResult> GetLatestSignal(string resolution = "15m", int period = 20, decimal multiplier = 2)
        {
            var endDate = DateTime.UtcNow;
            var fetcher = new HistoricalDataFetcher();

            int value = int.Parse(new string(resolution.Where(char.IsDigit).ToArray()));
            var startDate = DateTime.UtcNow.AddMinutes(-(1000 * value)); // max 2000

            List<Candlestick> historicalData = await fetcher.FetchCandles("BTCUSD", resolution, startDate, endDate);
           // var lastcandeltime = historicalData.FirstOrDefault().Time;
            historicalData.Reverse();

           // var lastCandelDetail = historicalData.FirstOrDefault();
           // var lastcandel = DateTimeOffset.FromUnixTimeSeconds(lastcandeltime).UtcDateTime;
            var istTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
           // var lastcandelT = TimeZoneInfo.ConvertTime(lastcandel, istTimeZone);
         

            var calculator = new BollingerBandCalculator(period, multiplier);
            var latestSignal = calculator.GetLatestSignal(historicalData);
            var SignalTime = DateTimeOffset.FromUnixTimeSeconds(latestSignal?.Time ?? 0).UtcDateTime;
            var SignaldateTime = TimeZoneInfo.ConvertTime(SignalTime, istTimeZone);

            var response = new
            {
                Signal = latestSignal?.SignalType.ToString(),
                SignalClosePrice = latestSignal?.Close,
                SignalTime = SignaldateTime
            };

            return Ok(response);
        }


        [HttpPost("PriceActionSignal")]
        public async Task<ActionResult> GetPriceActionTradeSignal(string resolution = "15m", int period = 20)
        {
            var endDate = DateTime.UtcNow;
            var fetcher = new HistoricalDataFetcher();

            int value = int.Parse(new string(resolution.Where(char.IsDigit).ToArray()));
            var startDate = DateTime.UtcNow.AddMinutes(-(1000 * value)); // max 2000

            List<Candlestick> historicalData = await fetcher.FetchCandles("BTCUSD", resolution, startDate, endDate);

            var strategy = new PriceActionStrategy(historicalData);
            var trend = strategy.GetTrendDirection();
            var (support, resistance) = strategy.GetSupportResistance(period);
            var breakout = strategy.IsBreakout();
            var pullback = strategy.IsPullback();
            var priceActionSignal = strategy.GetTradeSignalWithTimestamp();

            var istTimeZone1 = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            var dateTime = TimeZoneInfo.ConvertTime(DateTime.UtcNow, istTimeZone1);

            var SignalTime = DateTimeOffset.FromUnixTimeSeconds(priceActionSignal?.Timestamp ?? 0).UtcDateTime;
            var SignaldateTime = TimeZoneInfo.ConvertTime(SignalTime, istTimeZone1);

            var response = new
            {
                TradeSignal = trend,
                Support = support,
                Resistance = resistance,
                Breakout = breakout,
                Pullback = pullback,
                Time = SignaldateTime
            };

            return Ok(response);
        }
    }

}



