using System.Text.Json.Serialization;

public sealed class PredictRequest
{
    [JsonPropertyName("sensor_id")] public string SensorId { get; set; } = "";
    [JsonPropertyName("timestamps")] public string[] Timestamps { get; set; } = [];
    [JsonPropertyName("temperatures")] public double[] Temperatures { get; set; } = [];
    [JsonPropertyName("humidities")] public double[] Humidities { get; set; } = [];
    [JsonPropertyName("pressures")] public double[] Pressures { get; set; } = [];
    [JsonPropertyName("light_levels")] public double[] LightLevels { get; set; } = [];
}

public sealed class PredictResponse
{
    [JsonPropertyName("label")] public string Label { get; set; } = "";
    [JsonPropertyName("score")] public double Score { get; set; }
    [JsonPropertyName("predicted_temp_10m")] public double PredictedTemp10m { get; set; }
    [JsonPropertyName("explanation")] public string Explanation { get; set; } = "";
}