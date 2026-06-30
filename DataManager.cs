using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Core;

public static class DataManager
{
    private static SQLiteConnection? _db;
    private static readonly object _lock = new();

    private static readonly string DbPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "PansIoTassignment",
        "sensors.db"
    );

    public static string DatabasePath => DbPath;

    private static SQLiteConnection Db =>
        _db ?? throw new InvalidOperationException("Call DataManager.InitializeDatabase() first.");

    public static void InitializeDatabase()
    {
        lock (_lock)
        {
            if (_db != null) return;

            Directory.CreateDirectory(Path.GetDirectoryName(DbPath)!);
            _db = new SQLiteConnection(DbPath);

            Db.CreateTable<TemperatureSensor>();
            Db.CreateTable<HumiditySensor>();
            Db.CreateTable<PressureSensorData>();
            Db.CreateTable<LightSensor>();
        }
    }

    public static void ClearAll()
    {
        lock (_lock)
        {
            Db.DeleteAll<TemperatureSensor>();
            Db.DeleteAll<HumiditySensor>();
            Db.DeleteAll<PressureSensorData>();
            Db.DeleteAll<LightSensor>();
        }
    }

    public static void OnUpdate(TemperatureSensor data)
    {
        lock (_lock)
        {
            if (data.LastUpdated == default) data.LastUpdated = DateTime.UtcNow;
            data.IsValid = data.CurrentTemperature >= -40 && data.CurrentTemperature <= 120;
            data.IsAlert = data.IsValid && (data.CurrentTemperature <= -5 || data.CurrentTemperature >= 30);
            Db.Insert(data);
        }
    }

    public static void OnUpdate(HumiditySensor data)
    {
        lock (_lock)
        {
            if (data.LastUpdated == default) data.LastUpdated = DateTime.UtcNow;
            data.IsValid = data.CurrentHumidity >= 0 && data.CurrentHumidity <= 100;
            data.IsAlert = data.IsValid && (data.CurrentHumidity <= 20 || data.CurrentHumidity >= 80);
            Db.Insert(data);
        }
    }

    public static void OnUpdate(PressureSensorData data)
    {
        lock (_lock)
        {
            if (data.Timestamp == default) data.Timestamp = DateTime.UtcNow;
            data.IsValid = data.Value >= 800 && data.Value <= 1200;
            data.IsAlert = data.IsValid && (data.Value <= 900 || data.Value >= 1100);
            Db.Insert(data);
        }
    }

    public static void OnUpdate(LightSensor data)
    {
        lock (_lock)
        {
            if (data.LastUpdated == default) data.LastUpdated = DateTime.UtcNow;
            data.IsValid = data.CurrentLightLevel >= 0;
            data.IsAlert = data.IsValid && data.CurrentLightLevel <= 10;
            Db.Insert(data);
        }
    }

    public static List<TemperatureSensor> GetTemperatureReadings()
    {
        lock (_lock) return Db.Table<TemperatureSensor>().ToList();
    }

    public static List<HumiditySensor> GetHumidityReadings()
    {
        lock (_lock) return Db.Table<HumiditySensor>().ToList();
    }

    public static List<PressureSensorData> GetPressureReadings()
    {
        lock (_lock) return Db.Table<PressureSensorData>().ToList();
    }

    public static List<LightSensor> GetLightReadings()
    {
        lock (_lock) return Db.Table<LightSensor>().ToList();
    }
}