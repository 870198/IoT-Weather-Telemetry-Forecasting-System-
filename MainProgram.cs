using Core;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

DataManager.InitializeDatabase();
Console.WriteLine("Dashboard DB: " + DataManager.DatabasePath);

string outputDir = AppContext.BaseDirectory;
string sensorId = "MKR1010";

string tempPng = Path.Combine(outputDir, "temperature.png");
string humPng = Path.Combine(outputDir, "humidity.png");
string presPng = Path.Combine(outputDir, "pressure.png");

var ml = new PredictionClient();

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (s, e) => { e.Cancel = true; cts.Cancel(); };

Console.WriteLine("Viewer running. Start TelemetryServer + Python, then feed telemetry.");

bool mlOk = false;
DateTime nextHealth = DateTime.UtcNow;

while (!cts.IsCancellationRequested)
{
    GraphManager.SaveTemperaturePlotPng(tempPng, sensorId);
    GraphManager.SaveHumidityPlotPng(humPng, sensorId);
    GraphManager.SavePressurePlotPng(presPng, sensorId);

    if (DateTime.UtcNow >= nextHealth)
    {
        mlOk = await ml.HealthAsync();
        Console.WriteLine("ML health: " + mlOk);
        nextHealth = DateTime.UtcNow.AddSeconds(30);
    }

    if (mlOk)
    {
        var req = FeatureBuilder.BuildPredictRequest(sensorId, 20);
        if (req.Temperatures.Length >= 2)
        {
            var pred = await ml.PredictAsync(req);
            if (pred != null)
                Console.WriteLine($"{DateTime.UtcNow:O} :: {pred.Label} :: {pred.Explanation}");
        }
        else
        {
            Console.WriteLine($"{DateTime.UtcNow:O} :: Not enough data yet for prediction");
        }
    }

    await Task.Delay(3000, cts.Token);
}