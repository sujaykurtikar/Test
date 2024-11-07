using System;
using System.Collections.Generic;
using System.Linq;

public class IndicatorService
{
    // HMA (Hull Moving Average)
    public static List<double> CalculateWMA(List<double> src, int length)
    {
        var result = new List<double>();

        if (src.Count < length)
        {
            // Not enough data to calculate WMA; fill with 0s or handle as needed
            result.Add(0.0);
            return result;
        }

        for (int i = 0; i <= src.Count - length; i++)
        {
            var subset = src.Skip(i).Take(length).ToArray();
            var weightSum = subset.Select((val, idx) => val * (idx + 1)).Sum();
            var divisor = Enumerable.Range(1, length).Sum();
            result.Add(weightSum / divisor);
        }

        return result;
    }


    // EHMA (Exponential Hull Moving Average)
    public static double CalculateEHMA(List<double> src, int length)
    {
        var sqrtLength = (int)Math.Round(Math.Sqrt(length));

        var ema1 = CalculateEMA(src, length);
        var ema2 = CalculateEMA(src, length);

        var diffEma = new List<double>();

        for (int i = 0; i < src.Count; i++)
        {
            diffEma.Add(2 * ema1[i] - ema2[i]);
        }

        return CalculateEMA(diffEma, sqrtLength).Last();
    }

    // THMA (Triangular Hull Moving Average)
    public static double CalculateTHMA(List<double> src, int length)
    {
        var thirdLength = length / 3;
        var halfLength = length / 2;

        var wma1 = CalculateWMA(src, thirdLength);
        var wma2 = CalculateWMA(src, halfLength);
        var wma3 = CalculateWMA(src, length);

        var diffWma = new List<double>();

        for (int i = 0; i < src.Count; i++)
        {
            diffWma.Add(wma1[i] * 3 - wma2[i] - wma3[i]);
        }

        return CalculateWMA(diffWma, length).Last();
    }

    // WMA (Weighted Moving Average)
 public static double CalculateHMA(List<double> src, int length)
{
    if (src.Count < length) 
    {
        // Not enough data to calculate HMA for this length
        return 0.0;
    }

    int halfLength = length / 2;
    int sqrtLength = (int)Math.Round(Math.Sqrt(length));

    var wma1 = CalculateWMA(src, halfLength);
    var wma2 = CalculateWMA(src, length);
    var diffWma = new List<double>();

    for (int i = 0; i < wma1.Count; i++)
    {
        diffWma.Add(2 * wma1[i] - wma2[i]);
    }

    return CalculateWMA(diffWma, sqrtLength).LastOrDefault();
}


    // EMA (Exponential Moving Average)
    public static List<double> CalculateEMA(List<double> src, int length)
    {
        var ema = new List<double>();
        double multiplier = 2.0 / (length + 1);

        for (int i = 0; i < src.Count; i++)
        {
            if (i == 0)
            {
                ema.Add(src[i]);
            }
            else
            {
                double value = (src[i] - ema[i - 1]) * multiplier + ema[i - 1];
                ema.Add(value);
            }
        }

        return ema;
    }
}
