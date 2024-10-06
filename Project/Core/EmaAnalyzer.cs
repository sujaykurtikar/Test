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

        // Check for latest crossover
        for (int i = emaShort.Count - 1; i > 0; i--)
        {
            if (IsCrossover(emaShort[i - 1], emaShort[i], emaLong[i - 1], emaLong[i], out string crossoverType))
            {
                // Calculate exact crossover price
                decimal crossoverPrice = CalculateExactCrossoverPrice(candles[i - 1], candles[i], emaShort[i - 1].Value, emaShort[i].Value, emaLong[i - 1].Value, emaLong[i].Value);
                return (true, crossoverType, crossoverPrice, candles[i]);
            }
        }

        return (false, "None", 0m, null);
    }

    private static bool IsCrossover((long Timestamp, decimal Value) prevShort, (long Timestamp, decimal Value) currentShort, (long Timestamp, decimal Value) prevLong, (long Timestamp, decimal Value) currentLong, out string crossoverType)
    {
        crossoverType = "";
        if (prevShort.Value < prevLong.Value && currentShort.Value > currentLong.Value)
        {
            crossoverType = "Bullish";
            return true;
        }
        else if (prevShort.Value > prevLong.Value && currentShort.Value < currentLong.Value)
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
