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
    public double Ema1Angle { get; set; }
    public double Ema2Angle { get; set; }
    public decimal CrossoverCandleOpen { get; set; }
    public decimal CrossoverCandleClose { get; set; }
    public decimal CrossoverCandleHigh { get; set; }
    public decimal CrossoverCandleLow { get; set; }
}


public class EmaAnalyzer
{
    public static Dictionary<string, List<(long Timestamp, decimal Value)>> CalculateEma(List<Candlestick> candles, int shortTerm, int longTerm)
    {
        var emas = new Dictionary<string, List<(long, decimal)>>()
        {
            { "Short", new List<(long, decimal)>() },
            { "Long", new List<(long, decimal)>() }
        };

        // Calculate Short-Term EMA
        var shortMultiplier = 2m / (shortTerm + 1);
        decimal shortEmaPrev = candles.Take(shortTerm).Average(c => c.Close); // Initial EMA
        emas["Short"].Add((candles[shortTerm - 1].Time, shortEmaPrev));

        for (int i = shortTerm; i < candles.Count; i++)
        {
            shortEmaPrev = ((candles[i].Close - shortEmaPrev) * shortMultiplier) + shortEmaPrev;
            emas["Short"].Add((candles[i].Time, shortEmaPrev));
        }

        // Calculate Long-Term EMA
        var longMultiplier = 2m / (longTerm + 1);
        decimal longEmaPrev = candles.Take(longTerm).Average(c => c.Close); // Initial EMA
        emas["Long"].Add((candles[longTerm - 1].Time, longEmaPrev));

        for (int i = longTerm; i < candles.Count; i++)
        {
            longEmaPrev = ((candles[i].Close - longEmaPrev) * longMultiplier) + longEmaPrev;
            emas["Long"].Add((candles[i].Time, longEmaPrev));
        }

        return emas;
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

        var emaKeys = emas.Keys.ToList();
        if (emaKeys.Count < 2)
        {
            return crossovers; // Not enough EMAs to compare
        }

        var ema1 = emas[emaKeys[0]];
        var ema2 = emas[emaKeys[1]];

        var angle1 = angles.ContainsKey(emaKeys[0]) ? angles[emaKeys[0]] : new List<(long Timestamp, decimal?)>();
        var angle2 = angles.ContainsKey(emaKeys[1]) ? angles[emaKeys[1]] : new List<(long Timestamp, decimal?)>();

        var combined = from ema1Item in ema1
                       join ema2Item in ema2 on ema1Item.Timestamp equals ema2Item.Timestamp
                       join a1 in angle1 on ema1Item.Timestamp equals a1.Timestamp into a1Group
                       from a1 in a1Group.DefaultIfEmpty()
                       join a2 in angle2 on ema2Item.Timestamp equals a2.Timestamp into a2Group
                       from a2 in a2Group.DefaultIfEmpty()
                       select new
                       {
                           ema1Item.Timestamp,
                           EMA1 = ema1Item.Value,
                           EMA2 = ema2Item.Value,
                           Angle1 = a1.Item2 ?? 0m,
                           Angle2 = a2.Item2 ?? 0m
                       };

        // Iterate through the combined data to detect crossovers
        var combinedList = combined.ToList();
        for (int i = 1; i < combinedList.Count; i++)
        {
            var previousItem = combinedList[i - 1];
            var currentItem = combinedList[i];

            bool wasBullish = previousItem.EMA1 > previousItem.EMA2;
            bool isBullish = currentItem.EMA1 > currentItem.EMA2;

            if (wasBullish && !isBullish)
            {
                // Bearish crossover detected
                crossovers.Add((currentItem.Timestamp, "Bearish", (currentItem.Angle1 + currentItem.Angle2) / 2));
            }
            else if (!wasBullish && isBullish)
            {
                // Bullish crossover detected
                crossovers.Add((currentItem.Timestamp, "Bullish", (currentItem.Angle1 + currentItem.Angle2) / 2));
            }
        }

        return crossovers;
    }

    public static (int latestCrossoverIndex, string latestCrossoverType, decimal latestEma1Angle, decimal latestEma2Angle, Candlestick latestCrossoverCandle) IdentifyLatestCrossover(List<Candlestick> candles, int shortTerm, int longTerm)
    {
        // Step 1: Calculate EMAs and Angles
        var emas = CalculateEma(candles, shortTerm, longTerm);
        var emaAngles = CalculateEmaAngles(emas);

        // Step 2: Identify Crossovers
        var crossovers = IdentifyEmaCrossoversAndAngles(emas, emaAngles);

        // Step 3: Find the latest crossover
        if (crossovers.Any())
        {
            var latestCrossover = crossovers.Last();
            var latestCrossoverIndex = candles.FindIndex(c => c.Time == latestCrossover.Timestamp);
            var latestCrossoverType = latestCrossover.Type;
            var latestEma1Angle = emaAngles["Short"].FirstOrDefault(a => a.Timestamp == latestCrossover.Timestamp).Item2 ?? 0m;
            var latestEma2Angle = emaAngles["Long"].FirstOrDefault(a => a.Timestamp == latestCrossover.Timestamp).Item2 ?? 0m;
            var latestCrossoverCandle = candles[latestCrossoverIndex];

            return (latestCrossoverIndex, latestCrossoverType, latestEma1Angle, latestEma2Angle, latestCrossoverCandle);
        }

        // Return default values if no crossover is found
        return (-1, "None", 0m, 0m, null);
    }



public static decimal? FindCrossoverPoint(List<Candlestick> candlesticks, List<decimal> emaShort, List<decimal> emaLong)
    {
        for (int i = 1; i < candlesticks.Count; i++)
        {
            // Check for a crossover between two consecutive candles
            if ((emaShort[i - 1] < emaLong[i - 1] && emaShort[i] > emaLong[i]) ||
                (emaShort[i - 1] > emaLong[i - 1] && emaShort[i] < emaLong[i]))
            {
                // Interpolation between candlestick i-1 and i
                decimal closeA = candlesticks[i - 1].Close;
                decimal closeB = candlesticks[i].Close;

                decimal emaShortA = emaShort[i - 1];
                decimal emaShortB = emaShort[i];
                decimal emaLongA = emaLong[i - 1];
                decimal emaLongB = emaLong[i];

                decimal numerator = emaLongA - emaShortA;
                decimal denominator = (emaShortB - emaShortA) - (emaLongB - emaLongA);

                if (denominator != 0)
                {
                    decimal priceCrossover = closeA + ((numerator / denominator) * (closeB - closeA));
                    return priceCrossover;
                }
            }
        }

        // Return null if no crossover found
        return null;
    }
}
