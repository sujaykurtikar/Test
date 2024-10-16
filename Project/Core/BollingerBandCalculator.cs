using System;
using System.Collections.Generic;
using System.Linq;

public enum TradeSignal
{
    Buy,
    Sell,
    NoTrade
}

public class BollingerBands
{
    public decimal UpperBand { get; set; }
    public decimal MiddleBand { get; set; }
    public decimal LowerBand { get; set; }
}

public class BollingerBandCalculator
{
    private readonly int _period;
    private readonly decimal _multiplier;
    private readonly decimal _squeezeThreshold;

    public BollingerBandCalculator(int period = 20, decimal multiplier = 2, decimal squeezeThreshold = 0.01m)
    {
        _period = period;
        _multiplier = multiplier;
        _squeezeThreshold = squeezeThreshold;
    }

    public List<BollingerBands> CalculateBollingerBands(List<Candlestick> candlesticks)
    {
        return Enumerable.Range(0, candlesticks.Count - _period + 1)
            .Select(i =>
            {
                var window = candlesticks.Skip(i).Take(_period).Select(c => c.Close).ToList();
                var mean = window.Average();
                var stdDev = (decimal)Math.Sqrt(window.Select(x => Math.Pow((double)(x - mean), 2)).Average());

                return new BollingerBands
                {
                    MiddleBand = mean,
                    UpperBand = mean + _multiplier * stdDev,
                    LowerBand = mean - _multiplier * stdDev
                };
            })
            .ToList();
    }

    public bool IsBollingerBandSqueeze(List<BollingerBands> bandsList)
    {
        if (bandsList.Count < 2)
            return false;

        var latestBand = bandsList.Last();
        decimal bandWidth = (latestBand.UpperBand - latestBand.LowerBand) / latestBand.MiddleBand;
        return bandWidth < _squeezeThreshold;
    }

    public TradeSignal GenerateSignal(Candlestick currentCandle, BollingerBands currentBand, bool isSqueeze)
    {
        if (isSqueeze)
        {
            if (currentCandle.Close > currentBand.LowerBand && currentCandle.Close < currentBand.MiddleBand)
                return TradeSignal.Buy;
            else if (currentCandle.Close < currentBand.UpperBand && currentCandle.Close > currentBand.MiddleBand)
                return TradeSignal.Sell;
        }
        return TradeSignal.NoTrade;
    }

    public List<(decimal Close, TradeSignal Signal)> GenerateSignals(List<Candlestick> candlesticks)
    {
        var bollingerBands = CalculateBollingerBands(candlesticks);
        bool isSqueeze = IsBollingerBandSqueeze(bollingerBands);

        return candlesticks
            .Skip(bollingerBands.Count - 1)
            .Select((currentCandle, index) =>
            {
                var currentBand = bollingerBands[Math.Min(index, bollingerBands.Count - 1)];
                var signal = GenerateSignal(currentCandle, currentBand, isSqueeze);
                return (Close: currentCandle.Close, Signal: signal);
            })
            .ToList();
    }
}
