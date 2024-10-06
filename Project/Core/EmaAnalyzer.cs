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
    public static (bool IsCrossover, string CrossoverType, decimal CrossoverPrice, Candlestick CrossoverCandle) IdentifyLatestCrossover(List<Candlestick> candles, int shortTerm, int longTerm)
    {
        // Calculate EMAs
        var emas = CalculateEma(candles, shortTerm, longTerm);
        var emaShort = emas["Short"];
        var emaLong = emas["Long"];

        // Check for the latest crossover using LINQ
        var crossover = emaShort
            .Select((shortEma, index) => new
            {
                ShortEma = shortEma,
                LongEma = emaLong.ElementAtOrDefault(index),
                PreviousShortEma = emaShort.ElementAtOrDefault(index - 1),
                PreviousLongEma = emaLong.ElementAtOrDefault(index - 1),
                Index = index
            })
            .Where(x => x.Index > 0 && x.PreviousShortEma != default && x.PreviousLongEma != default &&
                        IsCrossover(x.PreviousShortEma.Value, x.ShortEma.Value, x.PreviousLongEma.Value, x.LongEma.Value, out string crossoverType))
            .Select(x => new
            {
                IsCrossover = true,
                CrossoverType = x.PreviousShortEma.Value < x.PreviousLongEma.Value && x.ShortEma.Value > x.LongEma.Value ? "Bullish" :
                                x.PreviousShortEma.Value > x.PreviousLongEma.Value && x.ShortEma.Value < x.LongEma.Value ? "Bearish" : "None",
                CrossoverPrice = CalculateExactCrossoverPrice(candles[x.Index - 1], candles[x.Index], x.PreviousShortEma.Value, x.ShortEma.Value, x.PreviousLongEma.Value, x.LongEma.Value),
                CrossoverCandle = candles[x.Index]
            })
            .LastOrDefault(); // Get the latest crossover

        if (crossover != null)
        {
            return (crossover.IsCrossover, crossover.CrossoverType, crossover.CrossoverPrice, crossover.CrossoverCandle);
        }

        return (false, "None", 0m, null);
    }


    private static bool IsCrossover(decimal prevShort, decimal currentShort, decimal prevLong, decimal currentLong, out string crossoverType)
    {
        crossoverType = "";
        if (prevShort < prevLong && currentShort > currentLong)
        {
            crossoverType = "Bullish";
            return true;
        }
        else if (prevShort > prevLong && currentShort < currentLong)
        {
            crossoverType = "Bearish";
            return true;
        }
        return false;
    }

    private static decimal CalculateExactCrossoverPrice(Candlestick candleA, Candlestick candleB, decimal emaShortA, decimal emaShortB, decimal emaLongA, decimal emaLongB)
    {
        decimal numerator = emaLongA - emaShortA;
        decimal denominator = (emaShortB - emaShortA) - (emaLongB - emaLongA);

        if (denominator != 0)
        {
            decimal priceCrossover = candleA.Close + ((numerator / denominator) * (candleB.Close - candleA.Close));
            return priceCrossover;
        }

        // If division by zero, use the midpoint between candleA and candleB as fallback crossover price
        return (candleA.Close + candleB.Close) / 2;
    }

    private static Dictionary<string, List<(long Timestamp, decimal Value)>> CalculateEma(List<Candlestick> candles, int shortTerm, int longTerm)
    {
        var emas = new Dictionary<string, List<(long, decimal)>>()
        {
            { "Short", CalculateSingleEma(candles, shortTerm) },
            { "Long", CalculateSingleEma(candles, longTerm) }
        };

        return emas;
    }

    private static List<(long Timestamp, decimal Value)> CalculateSingleEma(List<Candlestick> candles, int period)
    {
        if (candles.Count < period)
            throw new ArgumentException("Insufficient candlesticks to calculate the EMA.");

        var emaList = new List<(long, decimal)>();
        var multiplier = 2m / (period + 1);
        decimal emaPrev = candles.Take(period).Average(c => c.Close);

        for (int i = period - 1; i < candles.Count; i++)
        {
            emaPrev = ((candles[i].Close - emaPrev) * multiplier) + emaPrev;
            emaList.Add((candles[i].Time, emaPrev));
        }

        return emaList;
    }
}
