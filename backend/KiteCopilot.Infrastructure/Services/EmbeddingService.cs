using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using KiteCopilot.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace KiteCopilot.Infrastructure.Services;

public class EmbeddingService : IEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly ILogger<EmbeddingService> _logger;
    private const int FallbackDimensions = 256;

    public EmbeddingService(HttpClient httpClient, IConfiguration config, ILogger<EmbeddingService> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new float[FallbackDimensions];
        }

        var apiKey = _config["Gemini:ApiKey"] ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            try
            {
                var requestBody = new
                {
                    model = "models/text-embedding-004",
                    content = new
                    {
                        parts = new[] { new { text = text.Trim() } }
                    }
                };

                var url = $"https://generativelanguage.googleapis.com/v1beta/models/text-embedding-004:embedContent?key={apiKey}";
                var response = await _httpClient.PostAsync(url, new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"));
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("embedding", out var embeddingEl) &&
                        embeddingEl.TryGetProperty("values", out var valuesEl))
                    {
                        var list = new List<float>();
                        foreach (var item in valuesEl.EnumerateArray())
                        {
                            list.Add(item.GetSingle());
                        }
                        return list.ToArray();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Gemini API embedding request failed, falling back to deterministic local embedding.");
            }
        }

        // Deterministic local feature embedding (normalized bag-of-ngrams with MD5 hashing)
        return GenerateDeterministicLocalEmbedding(text);
    }

    public float CosineSimilarity(float[] vectorA, float[] vectorB)
    {
        if (vectorA == null || vectorB == null || vectorA.Length == 0 || vectorB.Length == 0)
            return 0f;

        int len = Math.Min(vectorA.Length, vectorB.Length);
        float dotProduct = 0f;
        float normA = 0f;
        float normB = 0f;

        for (int i = 0; i < len; i++)
        {
            dotProduct += vectorA[i] * vectorB[i];
            normA += vectorA[i] * vectorA[i];
            normB += vectorB[i] * vectorB[i];
        }

        float denom = (float)(Math.Sqrt(normA) * Math.Sqrt(normB));
        return denom > 0f ? (dotProduct / denom) : 0f;
    }

    private static float[] GenerateDeterministicLocalEmbedding(string text)
    {
        var vector = new float[FallbackDimensions];
        var words = text.ToLowerInvariant().Split(new[] { ' ', '\r', '\n', '\t', ',', '.', ';', ':', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var word in words)
        {
            if (word.Length < 2) continue;
            using var md5 = MD5.Create();
            var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(word));
            int idx = Math.Abs(BitConverter.ToInt32(hash, 0)) % FallbackDimensions;
            int sign = (hash[4] % 2 == 0) ? 1 : -1;
            vector[idx] += sign * (float)Math.Log(1.0 + word.Length);
        }

        // L2 normalize
        float sumSquares = 0f;
        for (int i = 0; i < FallbackDimensions; i++)
        {
            sumSquares += vector[i] * vector[i];
        }

        float norm = (float)Math.Sqrt(sumSquares);
        if (norm > 0f)
        {
            for (int i = 0; i < FallbackDimensions; i++)
            {
                vector[i] /= norm;
            }
        }

        return vector;
    }
}
