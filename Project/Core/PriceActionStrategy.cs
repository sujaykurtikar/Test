using System;
using System.Collections.Generic;
using System.Linq;

public class PriceActionStrategy
{
    private List<Candlestick> _candlesticks;
    public class PriceActionResult
    {
        public string TradeSignal { get; set; }
        public decimal Support { get; set; }
        public decimal Resistance { get; set; }
        public bool Breakout { get; set; }
        public bool Pullback { get; set; }
        public DateTime SignalTime { get; set; }
        public string SignalDetails { get; set; }
    }
    public class PriceActionSignal
    {
        public string Signal { get; set; }
        public long Timestamp { get; set; }
    }

    public PriceActionStrategy(List<Candlestick> candlesticks)
    {
        _candlesticks = candlesticks;
    }

    // Find the index of the candlestick based on the given timestamp
    private int FindCandleIndexByTimestamp(long timestamp)
    {
        return _candlesticks.FindIndex(c => c.Time == timestamp);
    }

    // 1. Identify trend direction using a timestamp
    public string GetTrendDirection(long timestamp)
    {
        int index = FindCandleIndexByTimestamp(timestamp);

        if (index < 1 || index >= _candlesticks.Count)
            return "No trend data";

        var last = _candlesticks[index];
        var previous = _candlesticks[index - 1];

        if (last.Close > previous.Close && last.Low > previous.Low)
            return "Uptrend";
        else if (last.Close < previous.Close && last.High < previous.High)
            return "Downtrend";

        return "Range/No clear trend";
    }

    // 2. Detect support and resistance levels up to the given timestamp
    public (decimal Support, decimal Resistance) GetSupportResistance(long timestamp, int candleCount = 20)
    {
        int index = FindCandleIndexByTimestamp(timestamp);
        if (index == -1 || index < candleCount - 1)
            return (0, 0); // Return 0 if there's not enough data for support/resistance

        var recentCandlesticks = _candlesticks.Skip(index - candleCount + 1).Take(candleCount);
        decimal support = recentCandlesticks.Min(c => c.Low);
        decimal resistance = recentCandlesticks.Max(c => c.High);

        return (Support: support, Resistance: resistance);
    }

    // 3. Identify breakout signals at the given timestamp
    public bool IsBreakout(long timestamp, int candleCount = 20)
    {
        int index = FindCandleIndexByTimestamp(timestamp);
        if (index == -1) return false;

        var (support, resistance) = GetSupportResistance(timestamp, candleCount);
        var last = _candlesticks[index];

        return last.Close > resistance || last.Close < support;
    }

    // 4. Pullback confirmation at the given timestamp
    public bool IsPullback(long timestamp)
    {
        int index = FindCandleIndexByTimestamp(timestamp);
        if (index < 2) return false;

        var last = _candlesticks[index];
        var previous = _candlesticks[index - 1];
        var trendDirection = GetTrendDirection(timestamp);

        if (trendDirection == "Uptrend" && last.Close < previous.Close)
            return true; // Downward movement in uptrend indicates a pullback
        else if (trendDirection == "Downtrend" && last.Close > previous.Close)
            return true; // Upward movement in downtrend indicates a pullback

        return false;
    }

    // 5. Generate trading signals with timestamp
    public PriceActionSignal GetTradeSignalWithTimestamp(long timestamp, int candleCount = 20)
    {
        string trend = GetTrendDirection(timestamp);
        bool breakout = IsBreakout(timestamp, candleCount);
        bool pullback = IsPullback(timestamp);

        var lastCandle = _candlesticks.FirstOrDefault(c => c.Time == timestamp);
        if (lastCandle == null) return null;

        string tradeSignal = "No trade signal";

        if (trend == "Uptrend" && breakout)
            tradeSignal = "Buy (Breakout in Uptrend)";
        else if (trend == "Downtrend" && breakout)
            tradeSignal = "Sell (Breakout in Downtrend)";
        else if (trend == "Uptrend" && pullback)
            tradeSignal = "Buy (Pullback in Uptrend)";
        else if (trend == "Downtrend" && pullback)
            tradeSignal = "Sell (Pullback in Downtrend)";

        return new PriceActionSignal
        {
            Signal = tradeSignal,
            Timestamp = lastCandle.Time
        };
    }
}
