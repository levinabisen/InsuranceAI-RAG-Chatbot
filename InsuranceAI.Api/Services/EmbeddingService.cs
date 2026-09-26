using System.Text;
using System.Text.Json;

namespace InsuranceAI.Api.Services
{
    public class EmbeddingService : IEmbeddingService
    {
        private readonly HttpClient _httpClient;

        public EmbeddingService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<double>> GetEmbedding(string text)
        {
            var requestBody = new
            {
                model = "nomic-embed-text:latest",
                prompt = text
            };

            var json =
                JsonSerializer.Serialize(requestBody);

            var content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json"
            );

            var response =
                await _httpClient.PostAsync(
                    "http://localhost:11434/api/embeddings",
                    content
                );

            response.EnsureSuccessStatusCode();

            var result =
                await response.Content.ReadAsStringAsync();

            using var document =
                JsonDocument.Parse(result);

            var embedding =
                document.RootElement
                    .GetProperty("embedding")
                    .EnumerateArray()
                    .Select(x => x.GetDouble())
                    .ToList();

            return embedding;

        }
    }
}