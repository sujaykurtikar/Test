using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

//public class Candlestick
//{
//    public long Time { get; set; }
//    public decimal Open { get; set; }
//    public decimal High { get; set; }
//    public decimal Low { get; set; }
//    public decimal Close { get; set; }
//    public long Volume { get; set; }
//}

public class CandlestickPrediction
{
    [ColumnName("PredictedLabel")]
    public string TrendDirection { get; set; }
    public float[] Score { get; set; }
}

public class CandlestickModelManager
{
    private readonly MLContext mlContext;
    private ITransformer model;
    private List<Candlestick> candlestickData;
    private DateTime lastUpdateTime;

    public CandlestickModelManager()
    {
        mlContext = new MLContext();
        candlestickData = new List<Candlestick>();
        lastUpdateTime = DateTime.UtcNow; // Initialize to the current time
    }

    public async Task AddCandlestickDataAsync(IEnumerable<Candlestick> newData)
    {
        // Add new candlestick data
        candlestickData.AddRange(newData);
        lastUpdateTime = DateTime.UtcNow; // Update the last update time
    }

    public async Task RetrainModelAsync()
    {
        // Prepare data with trend labels
        var labeledData = LabelCandlestickData(candlestickData);
        var dataView = mlContext.Data.LoadFromEnumerable(labeledData);

        // Define pipeline for multiclass classification
        var pipeline = mlContext.Transforms.Conversion.MapValueToKey("Label", nameof(LabeledCandlestick.TrendDirection))
            .Append(mlContext.Transforms.Concatenate("Features", "Open", "High", "Low", "Close", "Volume"))
            .Append(mlContext.MulticlassClassification.Trainers.LightGbm())
            .Append(mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

        model = pipeline.Fit(dataView);

        // Save the updated model if needed
        mlContext.Model.Save(model, dataView.Schema, "updatedModel.zip");
    }

    public async Task CheckAndUpdateModelAsync(IEnumerable<Candlestick> historicalData)
    {
        // Check if 24 hours have passed since the last update
        if (DateTime.UtcNow - lastUpdateTime >= TimeSpan.FromHours(24))
        {
            // Call the method to add historical data
            await AddCandlestickDataAsync(historicalData);
            await RetrainModelAsync();
        }
    }

    public string PredictTrend(Candlestick inputData)
    {
        var predictionEngine = mlContext.Model.CreatePredictionEngine<Candlestick, CandlestickPrediction>(model);
        var prediction = predictionEngine.Predict(inputData);
        return prediction.TrendDirection;
    }

    private List<LabeledCandlestick> LabelCandlestickData(List<Candlestick> data)
    {
        if (data.Count < 2) return new List<LabeledCandlestick>();

        var labeledData = data.Skip(1)
            .Select((current, index) =>
            {
                var prevClose = data[index].Close; // Get the previous close price
                var currClose = current.Close; // Get the current close price

                string trendDirection = currClose > prevClose * 1.01m ? "Up" :
                                        currClose < prevClose * 0.99m ? "Down" :
                                        "Sideways";

                return new LabeledCandlestick
                {
                    Time = current.Time,
                    Open = current.Open,
                    High = current.High,
                    Low = current.Low,
                    Close = current.Close,
                    Volume = current.Volume,
                    TrendDirection = trendDirection
                };
            }).ToList();

        return labeledData;
    }
}


public class LabeledCandlestick : Candlestick
{
    public string TrendDirection { get; set; }
}
