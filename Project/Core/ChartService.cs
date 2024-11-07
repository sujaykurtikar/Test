//using System;
//using System.Collections.Generic;
//using System.IO;
//using OxyPlot;
//using OxyPlot.Axes;
//using OxyPlot.Series;
//using OxyPlot.SkiaSharp;

//public class ChartService
//{
//    public byte[] GenerateCandlestickChart(List<Candlestick> candlestickData)
//    {
//        var model = new PlotModel { Title = "Candlestick Chart" };
//        var series = new CandleStickSeries
//        {
//            Color = OxyColors.Black,
//            IncreasingColor = OxyColors.Green,
//            DecreasingColor = OxyColors.Red
//        };

//        foreach (var candle in candlestickData)
//        {
//            // Convert Unix timestamp (long) to DateTime
//            var dateTime = DateTimeOffset.FromUnixTimeMilliseconds(candle.Time).UtcDateTime;
//            series.Items.Add(new HighLowItem(
//                DateTimeAxis.ToDouble(dateTime),
//                (double)candle.High,
//                (double)candle.Low,
//                (double)candle.Open,
//                (double)candle.Close
//            ));
//        }

//        model.Series.Add(series);
//        model.Axes.Add(new DateTimeAxis { Position = AxisPosition.Bottom, StringFormat = "MM-dd" });
//        model.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "Price" });

//        using var stream = new MemoryStream();
//        var exporter = new PngExporter { Width = 800, Height = 600};
//        exporter.Export(model, stream);
//        return stream.ToArray();
//    }
//}
using System;
using System.Collections.Generic;
using System.IO;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.SkiaSharp;

public class ChartService
{
    public byte[] GenerateCandlestickChart(List<Candlestick> candlestickData)
    {
        var model = new PlotModel { Title = "Candlestick Chart" };
        var series = new CandleStickSeries
        {
            Color = OxyColors.Black,
            IncreasingColor = OxyColors.Green,
            DecreasingColor = OxyColors.Red
        };

        foreach (var candle in candlestickData)
        {
            // Convert Unix timestamp (long) to DateTime in UTC
            var utcDateTime = DateTimeOffset.FromUnixTimeMilliseconds(candle.Time).UtcDateTime;

            // Convert UTC to IST
            var istDateTime = utcDateTime.AddHours(5).AddMinutes(30);

            // Add the candlestick data to the series using IST time
            series.Items.Add(new HighLowItem(
                DateTimeAxis.ToDouble(istDateTime),
                (double)candle.High,
                (double)candle.Low,
                (double)candle.Open,
                (double)candle.Close
            ));
        }

        model.Series.Add(series);
        model.Axes.Add(new DateTimeAxis { Position = AxisPosition.Bottom, StringFormat = "HH:mm" }); //"MM-dd HH:mm 'IST'"
        model.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "Price" });

        using var stream = new MemoryStream();
        var exporter = new PngExporter { Width = 1000, Height = 600, };
        exporter.Export(model, stream);
        return stream.ToArray();
    }
}



