using InsuranceAI.Api.Model;
using InsuranceAI.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace InsuranceAI.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QdrantTestController : ControllerBase
    {
        private readonly IQdrantService _qdrantService;
        private readonly IEmbeddingService _embeddingService;

        public QdrantTestController(
            IQdrantService qdrantService,
            IEmbeddingService embeddingService)
        {
            _qdrantService = qdrantService;
            _embeddingService = embeddingService;
        }

        [HttpPost("insert")]
        public async Task<IActionResult> InsertChunk([FromBody] QdrantUpsertRequest request)
        {
            // Step 1: Embedding generate karo
            var embedding = await _embeddingService.GetEmbedding(request.Content);

            // Step 2: Qdrant me insert karo
            await _qdrantService.UpsertChunkAsync(
                request.Id,
                request.Content,
                embedding,
                request.DocumentName,
                request.PageNumber
            );

            return Ok(new { message = "Chunk inserted successfully" });
        }



        [HttpPost("search")]
        public async Task<IActionResult> Search([FromBody] string query)
        {
            // Step 1: Query ka embedding banao
            var queryEmbedding = await _embeddingService.GetEmbedding(query);

            // Step 2: Qdrant se top 3 similar chunks dhundo
            var results = await _qdrantService.SearchAsync(queryEmbedding, topK: 3);

            // Step 3: Result ko clean format me return karo
            var response = results.Select(r => new
            {
                Score = r.Score,
                Content = r.Payload["content"].StringValue,
                DocumentName = r.Payload["documentName"].StringValue,
                PageNumber = r.Payload["pageNumber"].IntegerValue
            });

            return Ok(response);
        }
    }

}
