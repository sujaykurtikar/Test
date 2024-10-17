using System;
using System.Collections.Generic;
using System.Linq;


public class PriceActionStrategy
{
    private List<Candlestick> _candlesticks;

    public class PriceActionSignal
    {
        public string Signal { get; set; }
        public long Timestamp { get; set; }
    }
    public PriceActionStrategy(List<Candlestick> candlesticks)
    {
        _candlesticks = candlesticks;
    }

    // 1. Identify trend direction
    public string GetTrendDirection()
    {
        if (_candlesticks.Count < 2)
            return "No trend data";

        var last = _candlesticks[^1];
        var previous = _candlesticks[^2];

        if (last.Close > previous.Close && last.Low > previous.Low)
            return "Uptrend";
        else if (last.Close < previous.Close && last.High < previous.High)
            return "Downtrend";

        return "Range/No clear trend";
    }

    // 2. Detect support and resistance levels
    public (decimal Support, decimal Resistance) GetSupportResistance(int candleCount = 20)
    {
        var recentCandlesticks = _candlesticks.TakeLast(candleCount); // Consider recent 20 candles
        decimal support = recentCandlesticks.Min(c => c.Low);
        decimal resistance = recentCandlesticks.Max(c => c.High);

        return (Support: support, Resistance: resistance);
    }

    // 3. Identify breakout signals
    public bool IsBreakout()
    {
        var (support, resistance) = GetSupportResistance();
        var last = _candlesticks[^1];

        return last.Close > resistance || last.Close < support;
    }

    // 4. Pullback confirmation
    public bool IsPullback()
    {
        if (_candlesticks.Count < 3) return false;

        var last = _candlesticks[^1];
        var previous = _candlesticks[^2];
        var trendDirection = GetTrendDirection();

        if (trendDirection == "Uptrend" && last.Close < previous.Close)
            return true; // Downward movement in uptrend indicates a pullback
        else if (trendDirection == "Downtrend" && last.Close > previous.Close)
            return true; // Upward movement in downtrend indicates a pullback

        return false;
    }

    // 5. Generate trading signals
    public PriceActionSignal GetTradeSignalWithTimestamp()
    {
        string trend = GetTrendDirection();
        bool breakout = IsBreakout();
        bool pullback = IsPullback();

        var lastCandle = _candlesticks[^1];
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
