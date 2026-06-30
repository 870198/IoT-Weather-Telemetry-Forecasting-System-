using SQLite;
using System;

namespace Core;

public class TemperatureSensor
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    public string SensorId { get; set; } = "";
    public double CurrentTemperature { get; set; }
    public DateTime LastUpdated { get; set; }
    public bool IsValid { get; set; } = true;
    public bool IsAlert { get; set; } = false;
}

public class HumiditySensor
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    public string SensorId { get; set; } = "";
    public double CurrentHumidity { get; set; }
    public DateTime LastUpdated { get; set; }
    public bool IsValid { get; set; } = true;
    public bool IsAlert { get; set; } = false;
}

public class PressureSensorData
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    public string SensorId { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public double Value { get; set; }
    public bool IsValid { get; set; } = true;
    public bool IsAlert { get; set; } = false;
}

public class LightSensor
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    public string SensorId { get; set; } = "";
    public int CurrentLightLevel { get; set; }
    public DateTime LastUpdated { get; set; }
    public bool IsValid { get; set; } = true;
    public bool IsAlert { get; set; } = false;
}