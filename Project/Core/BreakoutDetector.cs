using System;
using System.Collections.Generic;
using static PriceActionStrategy;


public class BreakoutDetector
{
    private List<Candlestick> candlesticks;
    private decimal trendlineSlope;
    private decimal trendlineIntercept;

    public BreakoutDetector(List<Candlestick> candlestickData)
    {
        candlesticks = candlestickData;

        // Initialize the trendline based on a basic linear regression using highs or closes
        CalculateTrendline();
    }

    // Simple method to calculate trendline using linear regression (on highs or closes)
    private void CalculateTrendline()
    {
        int n = candlesticks.Count;
        decimal sumX = 0, sumY = 0, sumXY = 0, sumX2 = 0;

        foreach (var candle in candlesticks)
        {
            sumX += candle.Time;
            sumY += candle.High; // Use 'Close' or 'High' depending on your trendline
            sumXY += candle.Time * candle.High;
            sumX2 += candle.Time * candle.Time;
        }

        // Calculate slope (m) and intercept (b) for trendline equation: y = mx + b
        trendlineSlope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
        trendlineIntercept = (sumY - trendlineSlope * sumX) / n;
    }

    // Method to check for breakout and provide signal
    public string CheckBreakout(Candlestick newCandle)
    {
        // Calculate expected trendline value for the current candle's time
        decimal expectedTrendlineValue = trendlineSlope * newCandle.Time + trendlineIntercept;
        var istTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
        var SignaldateTime = TimeZoneInfo.ConvertTime(DateTimeOffset.FromUnixTimeSeconds(newCandle.Time).UtcDateTime, istTimeZone);
        // Check if the close price crosses the trendline
        if (newCandle.Close > expectedTrendlineValue)
        {
            return $"Bullish breakout detected at time {SignaldateTime}, close: {newCandle.Close}";
        }
        else if (newCandle.Close < expectedTrendlineValue)
        {
            return $"Bearish breakout detected at time {SignaldateTime}, close: {newCandle.Close}";
        }

        return "No breakout detected";
    }

    private bool IsBreakout(Candlestick candle)
    {
        decimal expectedTrendlineValue = trendlineSlope * candle.Time + trendlineIntercept;

        // Check if the close price is outside of the trendline
        return candle.High > expectedTrendlineValue || candle.High < expectedTrendlineValue;
    }

    // Get all breakout points using LINQ
    public List<string> GetAllBreakouts()
    {
        var istTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
        
        return candlesticks
            .Where(candle => IsBreakout(candle))
            .Select(candle =>
            {
                decimal expectedTrendlineValue = trendlineSlope * candle.Time + trendlineIntercept;
                return candle.Close > expectedTrendlineValue
                    ? $"Bullish breakout at time {TimeZoneInfo.ConvertTime(DateTimeOffset.FromUnixTimeSeconds(candle.Time).UtcDateTime, istTimeZone)}, close: {candle.Close}"
                    : $"Bearish breakout at time {TimeZoneInfo.ConvertTime(DateTimeOffset.FromUnixTimeSeconds(candle.Time).UtcDateTime, istTimeZone)}, close: {candle.Close}";
            })
            .ToList();
    }
}

// Example usage:

