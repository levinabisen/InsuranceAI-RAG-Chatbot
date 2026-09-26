using InsuranceAI.Api.Data;
using InsuranceAI.Api.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

namespace InsuranceAI.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VectorSearchController : ControllerBase
    {
        private readonly AppDbContext _context;

        public VectorSearchController(AppDbContext context)
        {
            _context = context;
        }

        private async Task<string> AskLlama(string question, string context)
        {
            //        var prompt = $"""
            //Use the following context to answer the question.

            //Context:
            //{context}

            //Question:
            //{question}
            //""";

            var prompt = $"""
You are a helpful assistant.

Answer the question using ONLY the information provided in the context.

If the answer is not available in the context, say:
"I could not find the answer in the provided document."

Context:
{context}

Question:
{question}

Answer:
""";

            var requestBody = new
            {
                model = "llama3.2:3b",
                prompt = prompt,
                stream = false
            };

            var json = JsonSerializer.Serialize(requestBody);

            using var client = new HttpClient();

            var content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json"
            );

            var response = await client.PostAsync(
                "http://localhost:11434/api/generate",
                content
            );

            //var result = await response.Content.ReadAsStringAsync();

            //return result;

            var result = await response.Content.ReadAsStringAsync();

            var ollamaResponse =
                JsonSerializer.Deserialize<OllamaResponse>(result);

            return ollamaResponse?.Response ?? "No answer received.";
        }

        [HttpPost("seed")]
        public async Task<IActionResult> SeedDocuments()
        {
            var chunks = new List<string>
            {
                "Health insurance provides financial protection against medical and hospitalization expenses.",

                "To file an insurance claim, the policyholder needs to submit the required documents to the insurance company.",

                "Insurance premium is the amount of money a policyholder pays to keep an insurance policy active."
            };

            foreach (var chunk in chunks)
            {
                var embedding = await GetEmbedding(chunk);

                var document = new DocumentChunk
                {
                    Content = chunk,
                    EmbeddingJson = JsonSerializer.Serialize(embedding)
                };

                _context.DocumentChunks.Add(document);
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Documents and embeddings saved successfully."
            });
        }


        [HttpPost("search")]
        public async Task<IActionResult> Search(
    [FromBody] SearchRequest request)
        {
            // 1. User question ka embedding banao
            var questionEmbedding = await GetEmbedding(request.Query);

            // 2. Database se saare document chunks nikalo
            var documents = await _context.DocumentChunks.ToListAsync();

            // 3. Har document ke saath similarity calculate karo
            var results = documents
                .Select(document =>
                {
                    var documentEmbedding =
                        JsonSerializer.Deserialize<List<double>>(
                            document.EmbeddingJson
                        ) ?? new List<double>();

                    var similarity =
                        CosineSimilarity(
                            questionEmbedding,
                            documentEmbedding
                        );

                    return new
                    {
                        document.Id,
                        document.Content,
                        Similarity = similarity
                    };
                }).Where(x => x.Similarity >= 0.50)
    .OrderByDescending(x => x.Similarity)
    .Take(3)
    .ToList(); // this is for threshhold
            // OrderByDescending(x => x.Similarity)
            // .Take(3).ToList();/// for Top-k Similarity
            //.OrderByDescending(x => x.Similarity)
            //.Take(1).ToList(); /// for only one Similarity


           // var bestResult = results.FirstOrDefault();

            //if (bestResult == null)
            //{
            //    return NotFound("No relevant information found.");
            //}

            if (!results.Any())
            {
                return Ok(new
                {
                    question = request.Query,
                    answer = "I could not find relevant information in the provided document."
                });
            } /// Threshhold


            //var context = bestResult.Content;
            var context = string.Join(
    "\n\n---\n\n",
    results.Select(x => x.Content)
);

            var llamaResponse = await AskLlama(
                request.Query,
                context
            );

            return Ok(new
            {
                question = request.Query,
                context = context,
                answer = llamaResponse
            });

            //return Ok(results);
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

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(
                    $"Ollama error: {response.StatusCode} - {result}"
                );
            }

            var embeddingResponse =
                JsonSerializer.Deserialize<EmbeddingResponse>(result);

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

