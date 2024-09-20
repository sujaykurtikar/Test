public class VolumeDryUpStrategy
{
    public static bool IsVolumeDryingUp(List<Candlestick> candles, int lookbackPeriod, decimal dryUpThreshold = 0.5m)
    {
        var averageVolume = candles.Skip(candles.Count - lookbackPeriod).Average(c => c.Volume);
        var latestVolume = candles.Last().Volume;

        // Check if the latest volume is less than the dry-up threshold of average volume
        return latestVolume < Convert.ToDecimal( averageVolume) * dryUpThreshold;
    }

    public static bool IsBreakoutWithVolume(List<Candlestick> candles, int lookbackPeriod, decimal breakoutThreshold = 1.5m)
    {
        var averageVolume = candles.Skip(candles.Count - lookbackPeriod).Average(c => c.Volume);
        var latestVolume = candles.Last().Volume;
        var previousCandle = candles[candles.Count - 2];
        var latestCandle = candles.Last();

        // Check if the latest volume is greater than breakout threshold of average volume
        bool isVolumeSurge = latestVolume > Convert.ToDecimal(averageVolume) * breakoutThreshold;

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
            Console.WriteLine("Volume is drying up, waiting for a breakout...");
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
}
