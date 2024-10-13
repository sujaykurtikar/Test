public class VolumeDryUpStrategy
{
    public static bool IsVolumeDryingUp(List<Candlestick> candles, int lookbackPeriod, decimal dryUpThreshold = 0.5m)
    {
        if (candles.Count < lookbackPeriod) return false;

        // Calculate average volume over the lookback period
        var recentCandles = candles.Skip(candles.Count - lookbackPeriod).ToList();
        var averageVolume = recentCandles.Average(c => c.Volume);

        // Check for continuous decrease in volume
        bool isVolumeDecreasing = recentCandles
            .Take(lookbackPeriod - 1)
            .Zip(recentCandles.Skip(1), (prev, curr) => curr.Volume < prev.Volume)
            .All(decreasing => decreasing);

        // Check if the latest volume is below the dry-up threshold
        var latestVolume = candles.Last().Volume;
        return isVolumeDecreasing && latestVolume < Convert.ToDecimal(averageVolume) * dryUpThreshold;
    }

    public static bool IsBreakoutWithVolume(List<Candlestick> candles, int lookbackPeriod, decimal breakoutThreshold = 1.5m)
    {
        if (candles.Count < lookbackPeriod) return false;

        var averageVolume = candles.Skip(candles.Count - lookbackPeriod).Average(c => c.Volume);
        var latestVolume = candles.Last().Volume;
        var previousCandle = candles[candles.Count - 2];
        var latestCandle = candles.Last();

        // Check if the latest volume is greater than the breakout threshold of average volume
        bool isVolumeSurge = latestVolume >Convert.ToDecimal(averageVolume) * breakoutThreshold;

        // Check if price broke above resistance or below support
        bool isPriceBreakoutUp = latestCandle.Close > previousCandle.High;  // Resistance Breakout
        bool isPriceBreakoutDown = latestCandle.Close < previousCandle.Low;  // Support Breakout

        return isVolumeSurge && (isPriceBreakoutUp || isPriceBreakoutDown);
    }

    public static string GenerateTradeSignal(List<Candlestick> candles, int lookbackPeriod, decimal dryUpThreshold = 0.5m, decimal breakoutThreshold = 1.5m)
    {
        bool isDryingUp = IsVolumeDryingUp(candles, lookbackPeriod, dryUpThreshold);
        if (isDryingUp)
        {
           // Console.WriteLine("Volume is drying up, waiting for a breakout...");
        }

        bool isBreakout = IsBreakoutWithVolume(candles, lookbackPeriod, breakoutThreshold);
        if (isBreakout)
        {
            var latestCandle = candles.Last();
            var previousCandle = candles[candles.Count - 2];

            if (latestCandle.Close > previousCandle.High)
            {
                return "Buy";  // Breakout to the upside
            }
            else if (latestCandle.Close < previousCandle.Low)
            {
                return "Sell";  // Breakout to the downside
            }
        }

        return "No Signal";
    }

    public static (decimal profitTarget, decimal stopLoss) CalculateTradeParameters(List<Candlestick> candles, string tradeSignal)
    {
        var latestCandle = candles.Last();
        var previousCandle = candles[candles.Count - 2];

        decimal profitTarget = tradeSignal switch
        {
            "Buy" => latestCandle.Close + (latestCandle.Close - previousCandle.Low) * 1.5m, // Example profit target
            "Sell" => latestCandle.Close - (previousCandle.High - latestCandle.Close) * 1.5m, // Example profit target
            _ => 0m
        };

        decimal stopLoss = tradeSignal switch
        {
            "Buy" => previousCandle.Low, // Example stop-loss below the previous low
            "Sell" => previousCandle.High, // Example stop-loss above the previous high
            _ => 0m
        };

        return (profitTarget, stopLoss);
    }
}
