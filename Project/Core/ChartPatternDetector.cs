using System;
using System.Collections.Generic;
using System.Linq;



public class ChartPatternDetector
{
    private List<Candlestick> _candles;

    public ChartPatternDetector(List<Candlestick> candles)
    {
        _candles = candles;
    }

    public (long? breakoutTime, string side)? DetectLastDescendingTriangle()
    {
        long? lastBreakoutTime = null;
        string breakoutSide = null;

        for (int i = 4; i < _candles.Count; i++)
        {
            decimal supportLevel = _candles.Take(i).Min(c => c.Low);
            List<decimal> highs = _candles.Skip(i - 4).Take(4).Select(c => c.High).ToList();

            if (IsDescending(highs) && _candles[i].Close < supportLevel)
            {
                lastBreakoutTime = _candles[i].Time;
                breakoutSide = "Sell";
            }
        }
        return lastBreakoutTime.HasValue ? (lastBreakoutTime, breakoutSide) : null;
    }

    public (long? breakoutTime, string side)? DetectLastAscendingTriangle()
    {
        long? lastBreakoutTime = null;
        string breakoutSide = null;

        for (int i = 4; i < _candles.Count; i++)
        {
            decimal resistanceLevel = _candles.Take(i).Max(c => c.High);
            List<decimal> lows = _candles.Skip(i - 4).Take(4).Select(c => c.Low).ToList();

            if (IsAscending(lows) && _candles[i].Close > resistanceLevel)
            {
                lastBreakoutTime = _candles[i].Time;
                breakoutSide = "Buy";
            }
        }
        return lastBreakoutTime.HasValue ? (lastBreakoutTime, breakoutSide) : null;
    }

    public (long? breakoutTime, string side)? DetectLastDoubleTop()
    {
        long? lastBreakoutTime = null;
        string breakoutSide = null;

        for (int i = 4; i < _candles.Count; i++)
        {
            List<decimal> highs = _candles.Skip(i - 4).Take(4).Select(c => c.High).ToList();
            decimal recentHigh = highs.Last();
            decimal supportLevel = _candles.Take(i).Min(c => c.Low);

            if (highs.Count(h => h == recentHigh) == 2 && _candles[i].Close < supportLevel)
            {
                lastBreakoutTime = _candles[i].Time;
                breakoutSide = "Sell";
            }
        }
        return lastBreakoutTime.HasValue ? (lastBreakoutTime, breakoutSide) : null;
    }

    public (long? breakoutTime, string side)? DetectLastDoubleBottom()
    {
        long? lastBreakoutTime = null;
        string breakoutSide = null;

        for (int i = 4; i < _candles.Count; i++)
        {
            List<decimal> lows = _candles.Skip(i - 4).Take(4).Select(c => c.Low).ToList();
            decimal recentLow = lows.Last();
            decimal resistanceLevel = _candles.Take(i).Max(c => c.High);

            if (lows.Count(l => l == recentLow) == 2 && _candles[i].Close > resistanceLevel)
            {
                lastBreakoutTime = _candles[i].Time;
                breakoutSide = "Buy";
            }
        }
        return lastBreakoutTime.HasValue ? (lastBreakoutTime, breakoutSide) : null;
    }

    private bool IsDescending(List<decimal> highs)
    {
        for (int i = 1; i < highs.Count; i++)
        {
            if (highs[i] >= highs[i - 1]) return false;
        }
        return true;
    }

    private bool IsAscending(List<decimal> lows)
    {
        for (int i = 1; i < lows.Count; i++)
        {
            if (lows[i] <= lows[i - 1]) return false;
        }
        return true;
    }

    
}
