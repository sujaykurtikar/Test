using System;
using System.Collections.Generic;
using System.Linq;


public class ADXCalculator
{
    private const int Period = 14;


    public static List<(long Time, decimal ADX)> CalculateADXForCandles(List<Candlestick> candles)
    {
        if (candles.Count < Period + 1) throw new ArgumentException("Insufficient candle data.");

        // Calculate True Range (TR), +DM, and -DM using LINQ
        var trList = candles.Skip(1).Select((current, i) =>
        {
            var previous = candles[i];
            return Math.Max(
                current.High - current.Low,
                Math.Max(Math.Abs(current.High - previous.Close), Math.Abs(current.Low - previous.Close))
            );
        }).ToList();

        var plusDMList = candles.Skip(1).Select((current, i) =>
        {
            var previous = candles[i];
            return (current.High > previous.High && (current.High - previous.High) > (previous.Low - current.Low))
                ? current.High - previous.High : 0;
        }).ToList();

        var minusDMList = candles.Skip(1).Select((current, i) =>
        {
            var previous = candles[i];
            return (previous.Low > current.Low && (previous.Low - current.Low) > (current.High - previous.High))
                ? previous.Low - current.Low : 0;
        }).ToList();

        // Calculate averages for the first period
        decimal avgTR = trList.Take(Period).Average();
        decimal avgPlusDM = plusDMList.Take(Period).Average();
        decimal avgMinusDM = minusDMList.Take(Period).Average();

        // Initial ADX calculation
        var initialADX = CalculateInitialADX(avgTR, avgPlusDM, avgMinusDM);
        var adxList = new List<(long Time, decimal ADX)> { (candles[Period].Time, initialADX) };

        // Calculate ADX using LINQ
        for (int i = Period; i < candles.Count - 1; i++)
        {
            avgTR = (avgTR * (Period - 1) + trList[i]) / Period;
            avgPlusDM = (avgPlusDM * (Period - 1) + plusDMList[i]) / Period;
            avgMinusDM = (avgMinusDM * (Period - 1) + minusDMList[i]) / Period;

            var diPlus = (avgPlusDM / avgTR) * 100;
            var diMinus = (avgMinusDM / avgTR) * 100;

            var dx = Math.Abs(diPlus - diMinus) / (diPlus + diMinus) * 100;

            var adx = (adxList.Last().ADX * (Period - 1) + dx) / Period;
            adxList.Add((candles[i + 1].Time, adx));

            // Log intermediate values for comparison
            //Console.WriteLine($"Time: {candles[i + 1].Time}, TR: {trList[i]}, +DM: {plusDMList[i]}, -DM: {minusDMList[i]}, " +
            //                  $"+DI: {diPlus}, -DI: {diMinus}, DX: {dx}, ADX: {adx}");
        }

        return adxList;
    }

    private static decimal CalculateInitialADX(decimal avgTR, decimal avgPlusDM, decimal avgMinusDM)
    {
        var diPlus = (avgPlusDM / avgTR) * 100;
        var diMinus = (avgMinusDM / avgTR) * 100;

        return (Math.Abs(diPlus - diMinus) / (diPlus + diMinus)) * 100;
    }

    //public static string CheckAdxReversal(List<Candlestick> candles)
    //{
    //    var adxValues = CalculateADXForCandles(candles);

    //    if (adxValues.Count < 2)
    //    {
    //        return "Insufficient data for reversal check.";
    //    }

    //    // Get the last two ADX values based on timestamp
    //    var latestAdx = adxValues.OrderByDescending(x => x.Time).Take(2).ToList();
    //    var currentAdx = latestAdx[0];
    //    var previousAdx = latestAdx[1];

    //    bool isIncreasing = currentAdx.ADX > previousAdx.ADX;
    //    bool isDecreasingAndBelowThreshold = currentAdx.ADX < previousAdx.ADX && currentAdx.ADX < 12.5m;

    //    if (isDecreasingAndBelowThreshold)
    //    {
    //        return $"Reversal at Timestamp: {currentAdx.Time}";
    //    }

    //    return isIncreasing ? $"Increasing at Timestamp: {currentAdx.Time}" : $"Decreasing at Timestamp: {currentAdx.Time}";
    //}

    public static string CheckAdxReversal(List<(long Time, decimal ADX)> adxValues)
    {
        if (adxValues.Count < 2)
        {
            return "Insufficient data for reversal check.";
        }

        // Sort by timestamp and take the latest two entries
        var latestAdxValues = adxValues.OrderByDescending(x => x.Time).Take(2).ToList();
        var currentAdx = latestAdxValues[0];
        var previousAdx = latestAdxValues[1];

        bool isIncreasing = currentAdx.ADX > previousAdx.ADX;
        bool isDecreasingAndBelowThreshold = currentAdx.ADX < previousAdx.ADX && currentAdx.ADX < 12.5m;

        if (isDecreasingAndBelowThreshold)
        {
            return "REV";
        }

        return isIncreasing ? "INC" : "DEC";
    }
}



