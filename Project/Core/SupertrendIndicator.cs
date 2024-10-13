public class SupertrendIndicator
{
    private readonly int _atrPeriod;
    private readonly decimal _multiplier;
    private List<decimal> atrValues = new List<decimal>();

    public SupertrendIndicator(int atrPeriod, decimal multiplier)
    {
        _atrPeriod = atrPeriod;
        _multiplier = multiplier;
    }

    public List<(decimal Supertrend, string Signal)> CalculateSupertrend(List<Candlestick> candlesticks)
    {
        var supertrendValues = new List<(decimal, string)>();
        decimal upperBand = 0, lowerBand = 0, supertrend = 0;
        string currentSignal = "Hold";  // Initial state: no action (Hold)

        for (int i = 1; i < candlesticks.Count; i++)
        {
            // Calculate True Range (TR)
            decimal tr = Math.Max(candlesticks[i].High - candlesticks[i].Low,
                Math.Max(Math.Abs(candlesticks[i].High - candlesticks[i - 1].Close),
                         Math.Abs(candlesticks[i].Low - candlesticks[i - 1].Close)));

            if (i >= _atrPeriod)
            {
                // ATR calculation using SMA over the specified period
                decimal atr = atrValues.Skip(Math.Max(0, atrValues.Count - _atrPeriod)).Average();
                atrValues.Add(tr);

                // Calculate upper and lower bands
                upperBand = (candlesticks[i].Close + (_multiplier * atr));
                lowerBand = (candlesticks[i].Close - (_multiplier * atr));

                // Generate Supertrend signal
                if (candlesticks[i].Close > supertrend)
                {
                    // Buy Signal Scenario
                    if (currentSignal != "Buy")
                    {
                        currentSignal = "Buy";
                    }
                    supertrend = lowerBand;
                }
                else if (candlesticks[i].Close < supertrend)
                {
                    // Sell Signal Scenario
                    if (currentSignal != "Sell")
                    {
                        currentSignal = "Sell";
                    }
                    supertrend = upperBand;
                }
                else
                {
                    currentSignal = "Hold"; // Maintain current position if no crossover
                }

                supertrendValues.Add((supertrend, currentSignal));
            }
            else
            {
                atrValues.Add(tr); // Initial ATR values
            }
        }

        return supertrendValues;
    }
}
