using System;
using System.Collections.Generic;
using System.Linq;



public class VolumeMovingAverageCalculator
{
    // Function to calculate Volume Moving Average
    public List<decimal> CalculateVolumeMovingAverage(List<Candlestick> candlesticks, int period)
    {
        List<decimal> volumeMovingAverages = new List<decimal>();

        for (int i = 0; i < candlesticks.Count; i++)
        {
            if (i >= period - 1)
            {
                // Calculate the sum of volumes over the given period
                long sumVolume = candlesticks.Skip(i - period + 1).Take(period).Sum(c => c.Volume);
                decimal averageVolume = (decimal)sumVolume / period;
                volumeMovingAverages.Add(averageVolume);
            }
            else
            {
                // Not enough data for the given period, add 0 or null
                volumeMovingAverages.Add(0);  // You can choose to add null if you want
            }
        }

        return volumeMovingAverages;
    }
}
