using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public sealed class PredictionClient
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;

    public PredictionClient(HttpClient http, string baseUrl = "http://127.0.0.1:8000")
    {
        _http = http;
        _baseUrl = baseUrl.TrimEnd('/');
    }

    public async Task<PredictResponse?> PredictAsync(PredictRequest req)
    {
        try
        {
            string json = JsonSerializer.Serialize(req);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var resp = await _http.PostAsync($"{_baseUrl}/predict", content);
            resp.EnsureSuccessStatusCode();

            string respJson = await resp.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<PredictResponse>(respJson);
        }
        catch (System.Exception ex)
        {
            Log.Error("PredictAsync failed", ex);
            return null;
        }
    }
}

public sealed class PredictRequest
{
    public string sensor_id { get; set; } = "";
    public string[] timestamps { get; set; } = [];
    public double[] temperatures { get; set; } = [];
    public double[] humidities { get; set; } = [];
    public double[] pressures { get; set; } = [];
    public double[] light_levels { get; set; } = [];
}

public sealed class PredictResponse
{
    public string label { get; set; } = "";
    public double score { get; set; }
    public double predicted_temp_10m { get; set; }
    public string explanation { get; set; } = "";
}