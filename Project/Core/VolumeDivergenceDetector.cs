public class VolumeDivergenceDetector
{
    private int _lookBackPeriod;

    public VolumeDivergenceDetector(int lookBackPeriod)
    {
        _lookBackPeriod = lookBackPeriod;
    }

    // Checks for a valid candlestick list
    private bool IsValidCandlestickList(List<Candlestick> candlesticks)
    {
        return candlesticks != null && candlesticks.Count > _lookBackPeriod;
    }

    // Determines if there is a significant volume difference
    private bool IsSignificantVolumeDifference(long earlierVolume, long recentVolume)
    {
        // Customize the threshold for significance as per your strategy
        return Math.Abs(recentVolume - earlierVolume) > earlierVolume * 0.2m; // 20% difference
    }

    // Detects bullish volume divergence
    public bool IsBullishVolumeDivergence(List<Candlestick> candlesticks)
    {
        if (!IsValidCandlestickList(candlesticks)) return false;

        int lastIndex = candlesticks.Count - 1;
        bool priceMakingLowerLows = false;
        bool volumeIncreasing = false;

        // Iterate over the range and look for divergence
        for (int i = lastIndex - _lookBackPeriod; i < lastIndex; i++)
        {
            // Check if price is making lower lows
            if (candlesticks[i].Low > candlesticks[i + 1].Low)
            {
                priceMakingLowerLows = true;
            }

            // Check if volume is increasing or showing divergence
            if (candlesticks[i].Volume < candlesticks[i + 1].Volume &&
                IsSignificantVolumeDifference(candlesticks[i].Volume, candlesticks[i + 1].Volume))
            {
                volumeIncreasing = true;
            }

            // If both conditions are met, return true for bullish divergence
            if (priceMakingLowerLows && volumeIncreasing)
            {
                return true;
            }
        }
        return false;
    }

    // Detects bearish volume divergence
    public bool IsBearishVolumeDivergence(List<Candlestick> candlesticks)
    {
        if (!IsValidCandlestickList(candlesticks)) return false;

        int lastIndex = candlesticks.Count - 1;
        bool priceMakingHigherHighs = false;
        bool volumeDecreasing = false;

        // Iterate over the range and look for divergence
        for (int i = lastIndex - _lookBackPeriod; i < lastIndex; i++)
        {
            // Check if price is making higher highs
            if (candlesticks[i].High < candlesticks[i + 1].High)
            {
                priceMakingHigherHighs = true;
            }

            // Check if volume is decreasing or showing divergence
            if (candlesticks[i].Volume > candlesticks[i + 1].Volume &&
                IsSignificantVolumeDifference(candlesticks[i + 1].Volume, candlesticks[i].Volume))
            {
                volumeDecreasing = true;
            }

            // If both conditions are met, return true for bearish divergence
            if (priceMakingHigherHighs && volumeDecreasing)
            {
                return true;
            }
        }
        return false;
    }
}
