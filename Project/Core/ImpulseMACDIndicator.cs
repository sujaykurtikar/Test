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
        public string Signal { get; set; } // Buy, Sell, or Hold
    }

    public List<ImpulseMacdResult> CalculateImpulseMACD(List<Candlestick> candles, int fastPeriod = 12, int slowPeriod = 26, int signalPeriod = 9, int emaPeriod = 20)
    {
        List<ImpulseMacdResult> results = new List<ImpulseMacdResult>();

        if (candles.Count < slowPeriod)
        {
          //  Console.WriteLine("Not enough data to calculate MACD.");
            return results;
        }

        decimal[] emaFast = CalculateEMA(candles, fastPeriod);
        decimal[] emaSlow = CalculateEMA(candles, slowPeriod);
        decimal[] emaSignal = CalculateEMA(candles.Skip(slowPeriod - 1).ToList(), signalPeriod); // Signal line calculation starts after slow EMA
        decimal[] emaImpulse = CalculateEMA(candles, emaPeriod);

        for (int i = slowPeriod - 1; i < candles.Count; i++) // Start from slowPeriod - 1
        {
            var macd = emaFast[i] - emaSlow[i];
            var signalLine = emaSignal[i - (slowPeriod - 1)]; // Adjust index for signal line
            var histogram = macd - signalLine;
            var ema = emaImpulse[i];

            // Determine Signal (Buy, Sell, Hold)
            string signal = DetermineSignal(macd, signalLine);

            // Debugging output
           // Console.WriteLine($"Time: {candles[i].Time}, MACD: {macd}, SignalLine: {signalLine}, Histogram: {histogram}, EMA: {ema}, Signal: {signal}");

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

    private string DetermineSignal(decimal macd, decimal signalLine)
    {
        if (macd > signalLine)
            return "Buy";
        else if (macd < signalLine)
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

        //Console.WriteLine($"Latest Signal on {latestResult.Time}: {latestResult.Signal}");
        return latestResult.Signal;
    }
}
