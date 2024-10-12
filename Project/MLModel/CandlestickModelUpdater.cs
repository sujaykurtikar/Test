using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// Define the Candlestick class


// Define the prediction result class
public class CandlestickPrediction
{
    [ColumnName("PredictedLabel")]
    public string TrendDirection { get; set; }
    public float[] Score { get; set; }
}

// Manage the candlestick model
public class CandlestickModelManager
{
    private readonly MLContext mlContext;
    private ITransformer model;
    private readonly ConcurrentBag<Candlestick> candlestickData; // Thread-safe collection
    private DateTime lastUpdateTime;

    public CandlestickModelManager()
    {
        mlContext = new MLContext();
        candlestickData = new ConcurrentBag<Candlestick>();
        lastUpdateTime = DateTime.UtcNow; // Initialize to the current time
    }

    // Add new candlestick data asynchronously
    public async Task AddCandlestickDataAsync(IEnumerable<Candlestick> newData)
    {
        foreach (var data in newData)
        {
            candlestickData.Add(data);
        }
        lastUpdateTime = DateTime.UtcNow; // Update the last update time
        Console.WriteLine("Added new candlestick data.");
    }

    // Retrain the model asynchronously
    public async Task RetrainModelAsync()
    {
        try
        {
            // Prepare data with trend labels
            var labeledData = LabelCandlestickData(candlestickData.ToList()); // Convert to list for processing
            var dataView = mlContext.Data.LoadFromEnumerable(labeledData);

            // Define pipeline for multiclass classification
            var pipeline = mlContext.Transforms.Conversion.MapValueToKey("Label", nameof(LabeledCandlestick.TrendDirection))
                .Append(mlContext.Transforms.Concatenate("Features", "Open", "High", "Low", "Close", "Volume"))
                .Append(mlContext.MulticlassClassification.Trainers.LightGbm())
                .Append(mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

            model = pipeline.Fit(dataView);

            // Save the updated model
            mlContext.Model.Save(model, dataView.Schema, "updatedModel.zip");
            Console.WriteLine("Model retrained and saved.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while retraining the model: {ex.Message}");
        }
    }

    // Check and update the model based on time criteria
    public async Task CheckAndUpdateModelAsync(IEnumerable<Candlestick> historicalData)
    {
        try
        {
            // Check if 24 hours have passed since the last update
            if (DateTime.UtcNow - lastUpdateTime >= TimeSpan.FromHours(24))
            {
                Console.WriteLine("Updating model with new historical data...");
                await AddCandlestickDataAsync(historicalData);
                await RetrainModelAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while checking and updating the model: {ex.Message}");
        }
    }

    // Predict trend direction based on new input data
    public string PredictTrend(Candlestick inputData)
    {
        if (model == null)
        {
            throw new InvalidOperationException("Model has not been trained yet.");
        }

        try
        {
            var predictionEngine = mlContext.Model.CreatePredictionEngine<Candlestick, CandlestickPrediction>(model);
            var prediction = predictionEngine.Predict(inputData);
            return prediction.TrendDirection;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred during prediction: {ex.Message}");
            return null;
        }
    }

    // Label candlestick data for model training
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

// Extend Candlestick with trend information
public class LabeledCandlestick : Candlestick
{
    public string TrendDirection { get; set; }
}
