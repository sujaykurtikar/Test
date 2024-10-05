using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using static Test_Project.Controllers.ControlController;

namespace Project.Controllers
{
    public class TrendlinesController : Controller
    {
        private readonly IConfiguration _appSettings;

        public TrendlinesController(IConfiguration configuration)
        {
            _appSettings = configuration;
        }

        [HttpGet("trendlines/visualize")]
        public async Task<IActionResult> VisualizeTrendlines()
        {
            // Fetch the trendline data
            var startDate = DateTime.UtcNow.AddMinutes(-6000);

            var endDate = DateTime.UtcNow;
            var fetcher = new HistoricalDataFetcher();

            var resolution = _appSettings.GetValue<string>("Resolution");// "3m";

            var symbol = "BTCUSD";

            List<Candlestick> historicalData = await fetcher.FetchCandles(symbol, resolution, startDate, endDate);
            var lastcandeltime = historicalData.FirstOrDefault().Time;
            historicalData.Reverse();


            List<Trendline> trendlines = IdentifyTrendlines(historicalData);

            // Return the HTML content with embedded trendline data
            string htmlContent = System.IO.File.ReadAllText("trendlines.html")
                .Replace("{{trendlines}}", JsonConvert.SerializeObject(trendlines));

            return Content(htmlContent, "text/html");
        }

        public class Trendline
        {
            public DataPoint Start { get; set; }
            public DataPoint End { get; set; }
        }

        public class DataPoint
        {
            public double X { get; set; } // Represents the index or time
            public double Y { get; set; } // Represents the price
        }
        private List<Trendline> IdentifyTrendlines(List<Candlestick> candles)
        {
            var trendlines = new List<Trendline>();

            // Example trendline calculation (implement your own logic here)
            decimal? lastLow = null;
            decimal? lastHigh = null;

            for (int i = 1; i < candles.Count; i++)
            {
                // Example for uptrend
                if (lastLow == null || candles[i].Low < lastLow)
                {
                    if (lastLow != null)
                    {
                        trendlines.Add(new Trendline
                        {
                            Start = new DataPoint { X = i - 1, Y = (double)lastLow },
                            End = new DataPoint { X = i, Y = (double)candles[i].Low }
                        });
                    }
                    lastLow = candles[i].Low;
                }

                // Example for downtrend
                if (lastHigh == null || candles[i].High > lastHigh)
                {
                    if (lastHigh != null)
                    {
                        trendlines.Add(new Trendline
                        {
                            Start = new DataPoint { X = i - 1, Y = (double)lastHigh },
                            End = new DataPoint { X = i, Y = (double)candles[i].High }
                        });
                    }
                    lastHigh = candles[i].High;
                }
            }

            return trendlines;
        }
    }
}
