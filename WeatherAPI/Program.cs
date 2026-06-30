using Core;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.Urls.Clear();
app.Urls.Add("http://0.0.0.0:5000");

DataManager.InitializeDatabase();
Console.WriteLine("TelemetryServer DB: " + DataManager.DatabasePath);

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapPost("/telemetry", (TelemetryDto dto) =>
{
    var now = DateTime.UtcNow;

    DataManager.OnUpdate(new TemperatureSensor
    {
        SensorId = dto.SensorId,
        CurrentTemperature = dto.Temperature,
        LastUpdated = now
    });

    DataManager.OnUpdate(new HumiditySensor
    {
        SensorId = dto.SensorId,
        CurrentHumidity = dto.Humidity,
        LastUpdated = now
    });

    DataManager.OnUpdate(new PressureSensorData
    {
        SensorId = dto.SensorId,
        Value = dto.Pressure,
        Timestamp = now
    });

    DataManager.OnUpdate(new LightSensor
    {
        SensorId = dto.SensorId,
        CurrentLightLevel = dto.Light,
        LastUpdated = now
    });

    Console.WriteLine($"{now:O} {dto.SensorId} T={dto.Temperature} H={dto.Humidity} P={dto.Pressure} L={dto.Light}");
    return Results.Ok(new { stored = true, at = now });
});

app.Run();

public sealed class TelemetryDto
{
    public string SensorId { get; set; } = "MKR1010";
    public double Temperature { get; set; }
    public double Humidity { get; set; }
    public double Pressure { get; set; }
    public int Light { get; set; }
}