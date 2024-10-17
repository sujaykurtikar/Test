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

public class Signal
{
    public long Time { get; set; }
    public decimal Close { get; set; }
    public TradeSignal SignalType { get; set; }
}

public class BollingerBandCalculator
{
    private readonly int _period;
    private readonly decimal _multiplier;

    public BollingerBandCalculator(int period = 20, decimal multiplier = 2)
    {
        _period = period;
        _multiplier = multiplier;
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

    public TradeSignal GenerateSignal(Candlestick currentCandle, BollingerBands currentBand)
    {
        // Buy condition: green candle starts outside the lower band and closes inside it
        if (currentCandle.Open < currentBand.LowerBand && currentCandle.Close > currentBand.LowerBand && currentCandle.Close > currentCandle.Open)
            return TradeSignal.Buy;

        // Sell condition: red candle starts outside the upper band and closes inside it
        if (currentCandle.Open > currentBand.UpperBand && currentCandle.Close < currentBand.UpperBand && currentCandle.Close < currentCandle.Open)
            return TradeSignal.Sell;

        return TradeSignal.NoTrade;
    }

    public Signal GetLatestSignal(List<Candlestick> candlesticks)
    {
        var bollingerBands = CalculateBollingerBands(candlesticks);

        // Loop from the latest candle backwards to find the latest non-NoTrade signal
        for (int i = candlesticks.Count - 1; i >= bollingerBands.Count - 1; i--)
        {
            var currentCandle = candlesticks[i];
            var currentBand = bollingerBands[i - (bollingerBands.Count - 1)];
            var signalType = GenerateSignal(currentCandle, currentBand);

            if (signalType != TradeSignal.NoTrade)
            {
                return new Signal
                {
                    Time = currentCandle.Time,
                    Close = currentCandle.Close,
                    SignalType = signalType
                };
            }
        }

        // If no buy/sell signal was found, return the latest with NoTrade status
        return new Signal
        {
            Time = candlesticks.Last().Time,
            Close = candlesticks.Last().Close,
            SignalType = TradeSignal.NoTrade
        };
    }
}
