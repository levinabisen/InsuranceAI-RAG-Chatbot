using InsuranceAI.Api.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace InsuranceAI.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmbeddingController : ControllerBase
    {

        [HttpPost]
        public async Task<IActionResult> CreateEmbedding([FromBody] EmbeddingRequest request)
        {
            var requestBody = new
            {
                model = "nomic-embed-text:latest",
                prompt = request.Text
            };

            var json = JsonSerializer.Serialize(requestBody);

            using var client = new HttpClient();

            var content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json"
            );

            var response = await client.PostAsync(
                "http://localhost:11434/api/embeddings",
                content
            );

            var result = await response.Content.ReadAsStringAsync();

            return Ok(result);
        }

        [HttpPost("similarity")]
        public async Task<IActionResult> CalculateSimilarity(
    [FromBody] SimilarityRequest request)
        {
            // First text ka embedding
            var embedding1 = await GetEmbedding(request.Text1);

            // Second text ka embedding
            var embedding2 = await GetEmbedding(request.Text2);

            // Dono vectors ki similarity
            var similarity = CosineSimilarity(embedding1, embedding2);

            return Ok(new
            {
                text1 = request.Text1,
                text2 = request.Text2,
                similarity = similarity
            });
        }

        private async Task<List<double>> GetEmbedding(string text)
        {
            var requestBody = new
            {
                model = "nomic-embed-text:latest",
                prompt = text
            };

            var json = JsonSerializer.Serialize(requestBody);

            using var client = new HttpClient();

            var content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json"
            );

            var response = await client.PostAsync(
                "http://localhost:11434/api/embeddings",
                content
            );

            var result = await response.Content.ReadAsStringAsync();

            var embeddingResponse =
                JsonSerializer.Deserialize<EmbeddingResponse>(result);

            // return embeddingResponse?.Embedding ?? new List<double>();

            if (embeddingResponse == null ||
    embeddingResponse.Embedding == null ||
    embeddingResponse.Embedding.Count == 0)
            {
                throw new Exception("Ollama returned an empty embedding.");
            }

            return embeddingResponse.Embedding;
        }

        private double CosineSimilarity(
     List<double> vectorA,
     List<double> vectorB)
        {
            if (vectorA.Count == 0 || vectorB.Count == 0)
            {
                throw new Exception("Embedding vector is empty.");
            }

            if (vectorA.Count != vectorB.Count)
            {
                throw new Exception("Embedding vectors have different sizes.");
            }

            double dotProduct = 0;
            double magnitudeA = 0;
            double magnitudeB = 0;

            for (int i = 0; i < vectorA.Count; i++)
            {
                dotProduct += vectorA[i] * vectorB[i];

                magnitudeA += vectorA[i] * vectorA[i];

                magnitudeB += vectorB[i] * vectorB[i];
            }

            if (magnitudeA == 0 || magnitudeB == 0)
            {
                throw new Exception("Cannot calculate similarity for zero vector.");
            }

            return dotProduct /
                   (Math.Sqrt(magnitudeA) * Math.Sqrt(magnitudeB));
        }

    }
}
