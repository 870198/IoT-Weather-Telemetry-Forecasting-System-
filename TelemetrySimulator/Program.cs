using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

var http = new HttpClient();
string url = "http://127.0.0.1:5000/telemetry";

string sensorId = "MKR1010";
int i = 0;

while (true)
{
    double t = 20 + Math.Sin(i / 8.0) * 3;
    double h = 55 + Math.Sin(i / 10.0) * 10;
    double p = 1013 + Math.Sin(i / 15.0) * 6;
    int light = 100 + i * 5;

    var payload = new
    {
        sensorId,
        temperature = Math.Round(t, 2),
        humidity = Math.Round(h, 2),
        pressure = Math.Round(p, 2),
        light
    };

    string json = JsonSerializer.Serialize(payload);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    try
    {
        var resp = await http.PostAsync(url, content);
        Console.WriteLine($"{DateTime.UtcNow:O} POST {(int)resp.StatusCode}");
    }
    catch (Exception ex)
    {
        Console.WriteLine("POST failed: " + ex.Message);
    }

    i++;
    await Task.Delay(2000);
}