using System;
using Core;
public static class DummyDataSource
{
    private static int _i = 0;

    public static (TemperatureSensor t, HumiditySensor h, PressureSensorData p) Next(string sensorId)
    {
        _i++;

        double temp = 20 + Math.Sin(_i / 8.0) * 3;
        double humidity = 55 + Math.Sin(_i / 10.0) * 10;
        double pressure = 1013 + Math.Sin(_i / 15.0) * 6;

        DateTime now = DateTime.UtcNow;

        return (
            new TemperatureSensor
            {
                SensorId = sensorId,
                CurrentTemperature = Math.Round(temp, 2),
                LastUpdated = now,
                IsValid = true
            },
            new HumiditySensor
            {
                SensorId = sensorId,
                CurrentHumidity = Math.Round(humidity, 2),
                LastUpdated = now,
                IsValid = true
            },
            new PressureSensorData
            {
                SensorId = sensorId,
                Value = Math.Round(pressure, 2),
                Timestamp = now,
                IsValid = true
            }
        );
    }
}