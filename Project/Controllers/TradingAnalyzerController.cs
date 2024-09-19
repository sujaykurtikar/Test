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


public class TradingAnalyzer
{
    public static EmaCrossoverResult IdentifyCrossover(List<Candlestick> candles, int emaPeriod1, int emaPeriod2)
    {
        // Calculate EMA for each period
        var ema1 = CalculateEma(candles, emaPeriod1);
        var ema2 = CalculateEma(candles, emaPeriod2);

        if (ema1.Count < 2 || ema2.Count < 2)
        {
            throw new InvalidOperationException("Not enough data to calculate EMA.");
        }

        // Get the latest EMA values
        decimal latestEma1 = ema1.Last();
        decimal previousEma1 = ema1[ema1.Count - 2];
        decimal latestEma2 = ema2.Last();
        decimal previousEma2 = ema2[ema2.Count - 2];

        // Calculate the angles of the EMAs
        double ema1Angle = CalculateAngle(previousEma1, latestEma1);
        double ema2Angle = CalculateAngle(previousEma2, latestEma2);

        // Determine if there is a crossover and find the crossover candle
        bool isCrossover = (previousEma1 < previousEma2 && latestEma1 > latestEma2) || (previousEma1 > previousEma2 && latestEma1 < latestEma2);
        string crossoverType = latestEma1 > latestEma2 ? "Bullish" : "Bearish";

        // Find the index of the crossover candle
        int crossoverIndex = candles.Count - 1;
        if (isCrossover)
        {
            crossoverIndex = candles.Count - 1; // Latest candle is considered for crossover.
        }

        var crossoverCandle = candles[crossoverIndex];

        return new EmaCrossoverResult
        {
            IsCrossover = isCrossover,
            CrossoverType = isCrossover ? crossoverType : null,
            Ema1Angle = ema1Angle,
            Ema2Angle = ema2Angle,
            CrossoverCandleOpen = crossoverCandle.Open,
            CrossoverCandleClose = crossoverCandle.Close,
            CrossoverCandleHigh = crossoverCandle.High,
            CrossoverCandleLow = crossoverCandle.Low
        };
    }


    // Function to calculate EMA for a given period
    public static List<decimal> CalculateEma(List<Candlestick> candles, int period)
    {
        List<decimal> ema = new List<decimal>();
        decimal multiplier = 2m / (period + 1);
        decimal emaPrev = candles.Take(period).Average(c => c.Close); // Initial EMA

        ema.Add(emaPrev);

        for (int i = period; i < candles.Count; i++)
        {
            emaPrev = ((candles[i].Close - emaPrev) * multiplier) + emaPrev;
            ema.Add(emaPrev);
        }

        return ema;
    }

    // Function to calculate angle between two points (two EMA values)
    public static double CalculateAngle(decimal previousEma, decimal latestEma)
    {
        return Math.Atan((double)(latestEma - previousEma)) * (180 / Math.PI); // Angle in degrees
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
