using Core;
using System;
using System.Linq;

public static class FeatureBuilder
{
    public static PredictRequest BuildPredictRequest(string sensorId, int n)
    {
        var temps = DataManager.GetTemperatureReadings()
            .Where(r => r.IsValid && r.SensorId == sensorId)
            .OrderBy(r => r.LastUpdated)
            .TakeLast(n)
            .ToList();

        var hums = DataManager.GetHumidityReadings()
            .Where(r => r.IsValid && r.SensorId == sensorId)
            .OrderBy(r => r.LastUpdated)
            .TakeLast(n)
            .ToList();

        var press = DataManager.GetPressureReadings()
            .Where(r => r.IsValid && r.SensorId == sensorId)
            .OrderBy(r => r.Timestamp)
            .TakeLast(n)
            .ToList();

        return new PredictRequest
        {
            SensorId = sensorId,
            Timestamps = temps.Select(t => t.LastUpdated.ToString("O")).ToArray(),
            Temperatures = temps.Select(t => t.CurrentTemperature).ToArray(),
            Humidities = hums.Select(h => h.CurrentHumidity).ToArray(),
            Pressures = press.Select(p => p.Value).ToArray(),
            LightLevels = Array.Empty<double>()
        };
    }
}