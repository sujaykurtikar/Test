public class BacktestResult
{
    public int TotalTrades { get; set; }
    public int WinningTrades { get; set; }
    public int LosingTrades { get; set; }
    public decimal TotalProfit { get; set; }
    public decimal MaxDrawdown { get; set; }
}

public class VolumeDryUpBacktest
{
    public static BacktestResult RunBacktest(List<Candlestick> candles, int lookbackPeriod = 20, decimal dryUpThreshold = 0.5m, decimal breakoutThreshold = 1.5m)
    {
        decimal initialCapital = 10000;  // Example starting capital
        decimal currentCapital = initialCapital;
        decimal maxCapital = initialCapital;
        decimal drawdown = 0;

        decimal positionSize = 0;  // How many units are bought/sold (position in the asset)
        decimal entryPrice = 0;
        int totalTrades = 0;
        int winningTrades = 0;
        int losingTrades = 0;
        decimal totalProfit = 0;

        bool inPosition = false;  // Track whether we are in a trade

        for (int i = lookbackPeriod; i < candles.Count; i++)
        {
            // Generate signal
            var tradeSignal = VolumeDryUpStrategy.GenerateTradeSignal(candles.Take(i + 1).ToList(), lookbackPeriod, dryUpThreshold, breakoutThreshold);

            if (tradeSignal == "Buy" && !inPosition)
            {
                // Enter a buy trade
                entryPrice = candles[i].Close;
                positionSize = currentCapital / entryPrice;  // Buy as much as capital allows
                inPosition = true;
                totalTrades++;
             //   Console.WriteLine($"Buy Signal: Enter trade at {entryPrice} on {candles[i].Time}");
            }
            else if (tradeSignal == "Sell" && inPosition)
            {
                // Exit the trade
                decimal exitPrice = candles[i].Close;
                decimal profit = (exitPrice - entryPrice) * positionSize;
                currentCapital += profit;
                totalProfit += profit;

                if (profit > 0)
                {
                    winningTrades++;
                }
                else
                {
                    losingTrades++;
                }

                inPosition = false;
                positionSize = 0;

                // Update drawdown and max capital
                maxCapital = Math.Max(maxCapital, currentCapital);
                drawdown = Math.Max(drawdown, maxCapital - currentCapital);

               // Console.WriteLine($"Sell Signal: Exit trade at {exitPrice} on {candles[i].Time}. Profit: {profit}");
            }
        }

        return new BacktestResult
        {
            TotalTrades = totalTrades,
            WinningTrades = winningTrades,
            LosingTrades = losingTrades,
            TotalProfit = totalProfit,
            MaxDrawdown = drawdown
        };
    }
}
