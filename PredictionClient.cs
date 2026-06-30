using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public sealed class PredictionClient
{
    private readonly HttpClient _http = new();
    private readonly string _baseUrl;

    public PredictionClient(string baseUrl = "http://127.0.0.1:8000")
    {
        _baseUrl = baseUrl.TrimEnd('/');
    }

    public async Task<bool> HealthAsync()
    {
        try { return (await _http.GetAsync($"{_baseUrl}/health")).IsSuccessStatusCode; }
        catch { return false; }
    }

    public async Task<PredictResponse?> PredictAsync(PredictRequest req)
    {
        try
        {
            string json = JsonSerializer.Serialize(req);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var resp = await _http.PostAsync($"{_baseUrl}/predict", content);
            string respJson = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) return null;
            return JsonSerializer.Deserialize<PredictResponse>(respJson);
        }
        catch { return null; }
    }
}