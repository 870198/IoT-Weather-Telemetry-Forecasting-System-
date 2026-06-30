using Core;
using ScottPlot;
using System.IO;
using System.Linq;

public static class GraphManager
{
    public static void SaveTemperaturePlotPng(string filePath, string sensorId)
    {
        var readings = DataManager.GetTemperatureReadings()
            .Where(r => r.IsValid && r.SensorId == sensorId)
            .OrderBy(r => r.LastUpdated)
            .ToList();

        var plot = new Plot();
        plot.Title("Temperature Over Time");
        plot.XLabel("Time");
        plot.YLabel("Temperature (°C)");

        if (readings.Count >= 2)
        {
            double[] xs = readings.Select(r => r.LastUpdated.ToOADate()).ToArray();
            double[] ys = readings.Select(r => r.CurrentTemperature).ToArray();
            plot.Add.Scatter(xs, ys);
            plot.Axes.DateTimeTicksBottom();
        }

        File.WriteAllBytes(filePath, plot.GetImageBytes(1000, 600, ImageFormat.Png));
    }

    public static void SaveHumidityPlotPng(string filePath, string sensorId)
    {
        var readings = DataManager.GetHumidityReadings()
            .Where(r => r.IsValid && r.SensorId == sensorId)
            .OrderBy(r => r.LastUpdated)
            .ToList();

        var plot = new Plot();
        plot.Title("Humidity Over Time");
        plot.XLabel("Time");
        plot.YLabel("Humidity (%)");

        if (readings.Count >= 2)
        {
            double[] xs = readings.Select(r => r.LastUpdated.ToOADate()).ToArray();
            double[] ys = readings.Select(r => r.CurrentHumidity).ToArray();
            plot.Add.Scatter(xs, ys);
            plot.Axes.DateTimeTicksBottom();
        }

        File.WriteAllBytes(filePath, plot.GetImageBytes(1000, 600, ImageFormat.Png));
    }

    public static void SavePressurePlotPng(string filePath, string sensorId)
    {
        var readings = DataManager.GetPressureReadings()
            .Where(r => r.IsValid && r.SensorId == sensorId)
            .OrderBy(r => r.Timestamp)
            .ToList();

        var plot = new Plot();
        plot.Title("Pressure Over Time");
        plot.XLabel("Time");
        plot.YLabel("Pressure");

        if (readings.Count >= 2)
        {
            double[] xs = readings.Select(r => r.Timestamp.ToOADate()).ToArray();
            double[] ys = readings.Select(r => r.Value).ToArray();
            plot.Add.Scatter(xs, ys);
            plot.Axes.DateTimeTicksBottom();
        }

        File.WriteAllBytes(filePath, plot.GetImageBytes(1000, 600, ImageFormat.Png));
    }
}