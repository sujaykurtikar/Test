using System;
using System.Collections.Generic;
using System.Linq;

public class Candlestick
{
    public long Time { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public long Volume { get; set; }
}

public class EmaCrossoverResult
{
    public bool IsCrossover { get; set; }
    public string CrossoverType { get; set; }
    public decimal CrossoverPrice { get; set; }
    public Candlestick CrossoverCandle { get; set; }
}

public class EmaAnalyzer
{
    public static Dictionary<string, List<(long Timestamp, decimal Value)>> CalculateEmas(List<Candlestick> candles, int shortTerm, int longTerm)
    {
        var emas = new Dictionary<string, List<(long, decimal)>>()
        {
            { "Short", new List<(long, decimal)>() },
            { "Long", new List<(long, decimal)>() }
        };

        CalculateEma(candles, shortTerm, emas["Short"]);
        CalculateEma(candles, longTerm, emas["Long"]);

        return emas;
    }

    private static void CalculateEma(List<Candlestick> candles, int period, List<(long Timestamp, decimal Value)> emaList)
    {
        decimal multiplier = 2.0m / (period + 1);
        decimal? previousEma = null;

        for (int i = 0; i < candles.Count; i++)
        {
            var closePrice = candles[i].Close;
            if (i < period - 1)
            {
                continue; // Not enough data for the initial EMA
            }

            if (previousEma == null)
            {
                // Calculate simple average for the first EMA point
                previousEma = candles.Skip(i - period + 1).Take(period).Average(c => c.Close);
            }
            else
            {
                // Calculate EMA based on previous EMA
                previousEma = ((closePrice - previousEma) * multiplier) + previousEma;
            }

            emaList.Add((candles[i].Time, previousEma.Value));
        }
    }

    public static Dictionary<string, List<(long Timestamp, decimal?)>> CalculateEmaAngles(Dictionary<string, List<(long Timestamp, decimal Value)>> emas)
    {
        var angles = new Dictionary<string, List<(long Timestamp, decimal?)>>()
        {
            { "Short", new List<(long, decimal?)>() },
            { "Long", new List<(long, decimal?)>() }
        };

        if (emas["Short"].Count > 1)
        {
            CalculateAngle(emas["Short"], angles["Short"]);
        }

        if (emas["Long"].Count > 1)
        {
            CalculateAngle(emas["Long"], angles["Long"]);
        }

        return angles;
    }

    private static void CalculateAngle(List<(long Timestamp, decimal Value)> emaData, List<(long Timestamp, decimal?)> angleList)
    {
        for (int i = 1; i < emaData.Count; i++)
        {
            var x1 = i - 1;
            var y1 = emaData[i - 1].Value;
            var x2 = i;
            var y2 = emaData[i].Value;

            var slope = (y2 - y1) / (x2 - x1);
            var angle = (decimal)(Math.Atan((double)slope) * (180 / Math.PI));

            angleList.Add((emaData[i].Timestamp, angle));
        }
    }

    public static List<(long Timestamp, string Type, decimal Angle)> IdentifyEmaCrossoversAndAngles(
       Dictionary<string, List<(long Timestamp, decimal Value)>> emas,
       Dictionary<string, List<(long Timestamp, decimal?)>> angles)
    {
        var crossovers = new List<(long Timestamp, string Type, decimal Angle)>();

        if (emas.Count < 2)
            return crossovers; // Ensure there are two EMAs to compare

        var shortEma = emas["Short"];
        var longEma = emas["Long"];
        var shortAngle = angles.ContainsKey("Short") ? angles["Short"] : new List<(long Timestamp, decimal?)>();
        var longAngle = angles.ContainsKey("Long") ? angles["Long"] : new List<(long Timestamp, decimal?)>();

        var combined = from sEma in shortEma
                       join lEma in longEma on sEma.Timestamp equals lEma.Timestamp
                       join sAngle in shortAngle on sEma.Timestamp equals sAngle.Timestamp into sAngleGroup
                       from sAngle in sAngleGroup.DefaultIfEmpty()
                       join lAngle in longAngle on lEma.Timestamp equals lAngle.Timestamp into lAngleGroup
                       from lAngle in lAngleGroup.DefaultIfEmpty()
                       select new
                       {
                           sEma.Timestamp,
                           ShortEma = sEma.Value,
                           LongEma = lEma.Value,
                           ShortAngle = sAngle.Item2 ?? 0m,
                           LongAngle = lAngle.Item2 ?? 0m
                       };

        for (int i = 1; i < combined.Count(); i++)
        {
            var prev = combined.ElementAt(i - 1);
            var current = combined.ElementAt(i);

            if (prev.ShortEma < prev.LongEma && current.ShortEma > current.LongEma)
            {
                crossovers.Add((current.Timestamp, "Bullish", current.ShortAngle));
            }
            else if (prev.ShortEma > prev.LongEma && current.ShortEma < current.LongEma)
            {
                crossovers.Add((current.Timestamp, "Bearish", current.ShortAngle));
            }
        }

        return crossovers;
    }

}

