using InsuranceAI.Api.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Qdrant;
using Qdrant.Client;

namespace InsuranceAI.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SemanticKernelTestController : ControllerBase
    {
        [HttpPost("ask")]
        public async Task<IActionResult> Ask([FromBody] string question)
        {
            // Step 1: Kernel banao, Ollama se connect karo
            var builder = Kernel.CreateBuilder();

            builder.AddOllamaChatCompletion(
                modelId: "llama3.2:3b",
                endpoint: new Uri("http://localhost:11434"));

            var kernel = builder.Build();

            // Step 2: Seedha prompt bhejo, answer lo
            var result = await kernel.InvokePromptAsync(question);

            return Ok(new { answer = result.ToString() });
        }

        // NAYA ENDPOINT — YAHI TUMHARI LINE AAYEGI
        [HttpPost("create-collection")]
        public async Task<IActionResult> CreateCollection()
        {
            var vectorStore = new QdrantVectorStore(
                new QdrantClient("localhost"),
                ownsClient: true);

            var collection = vectorStore.GetCollection<ulong, ChunkRecord>("document_chunks");

            await collection.EnsureCollectionExistsAsync();

            return Ok(new { message = "Collection ready via Semantic Kernel." });
        }
    }
}
