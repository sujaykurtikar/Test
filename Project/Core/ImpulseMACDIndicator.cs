using System;
using System.Collections.Generic;
using System.Linq;

public class ImpulseMACDIndicator
{

    public class ImpulseMacdResult
    {
        public long Time { get; set; }
        public decimal Macd { get; set; }
        public decimal SignalLine { get; set; }
        public decimal Histogram { get; set; }
        public decimal Ema { get; set; }
        public string Signal { get; set; }  // Buy, Sell, or Hold
    }

    public List<ImpulseMacdResult> CalculateImpulseMACD(List<Candlestick> candles, int fastPeriod = 12, int slowPeriod = 26, int signalPeriod = 9, int emaPeriod = 20)
    {
        List<ImpulseMacdResult> results = new List<ImpulseMacdResult>();

        decimal[] emaFast = CalculateEMA(candles, fastPeriod);
        decimal[] emaSlow = CalculateEMA(candles, slowPeriod);
        decimal[] emaSignal = CalculateEMA(candles, signalPeriod);
        decimal[] emaImpulse = CalculateEMA(candles, emaPeriod);

        for (int i = 0; i < candles.Count; i++)
        {
            var macd = emaFast[i] - emaSlow[i];
            var signalLine = emaSignal[i];
            var histogram = macd - signalLine;
            var ema = emaImpulse[i];

            // Determine Signal (Buy, Sell, Hold)
            string signal = DetermineSignal(histogram, ema);

            results.Add(new ImpulseMacdResult
            {
                Time = candles[i].Time,
                Macd = macd,
                SignalLine = signalLine,
                Histogram = histogram,
                Ema = ema,
                Signal = signal
            });
        }

        return results;
    }

    private decimal[] CalculateEMA(List<Candlestick> candles, int period)
    {
        decimal[] emaValues = new decimal[candles.Count];
        decimal multiplier = 2.0m / (period + 1);

        // Initial EMA is the average of the first period values
        emaValues[period - 1] = candles.Take(period).Average(c => c.Close);

        for (int i = period; i < candles.Count; i++)
        {
            emaValues[i] = (candles[i].Close - emaValues[i - 1]) * multiplier + emaValues[i - 1];
        }

        return emaValues;
    }

    private string DetermineSignal(decimal histogram, decimal ema)
    {
        if (histogram > 0 && ema > 0)
            return "Buy";
        else if (histogram < 0 && ema < 0)
            return "Sell";
        else
            return "Hold";
    }

    public string GetLatestImpulseMACDSignal(List<Candlestick> candles)
    {
        var impulseMacdResults = CalculateImpulseMACD(candles);

        // Get the latest result (last item in the list)
        var latestResult = impulseMacdResults.LastOrDefault();

        if (latestResult == null)
            return "No data available";

        Console.WriteLine($"Latest Signal on {latestResult.Time}: {latestResult.Signal}");
        return latestResult.Signal;
    }
}
