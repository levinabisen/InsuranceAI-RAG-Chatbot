using InsuranceAI.Api.Data;
using InsuranceAI.Api.Model;
using InsuranceAI.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Qdrant.Client;
using System.Text;
using System.Text.Json;

namespace InsuranceAI.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly AppDbContext _db;
        private readonly IEmbeddingService _embeddingService;
        private readonly IQdrantService _qdrantService;   // NAYA

        //new
        private readonly QdrantClient _client;
        
        private readonly int _topK;
        private readonly string _baseUrl;
        private readonly string _chatModel;

        private readonly double _temperature;

        private readonly double _similarityThreshold;

        public ChatController(
            IHttpClientFactory httpClientFactory,
            AppDbContext db, IEmbeddingService embeddingService, IQdrantService qdrantService, IOptions<QdrantOptions> options, IOptions<OllamaOptions> ollamaOptions, IOptions<RagOptions> ragOptions)
        {
            _httpClient = httpClientFactory.CreateClient();
            _db = db;
            _embeddingService = embeddingService;
            _qdrantService = qdrantService;

            var config = options.Value;
            _client = new QdrantClient(config.Host, config.Port);

            var config2 = ragOptions.Value;
            _topK = config2.TopK;
            _similarityThreshold = config2.SimilarityThreshold;

            var config3 = ollamaOptions.Value;
            _baseUrl = config3.BaseUrl;
            _chatModel = config3.ChatModel;
            _temperature = config3.Temperature;
            
        }

        //previous method
        //[HttpPost]
        //public async Task<IActionResult> Chat([FromBody] ChatRequest request)
        //{
        //    // 1. Previous messages database se nikalo
        //    var chatHistory = await _db.ChatMessages
        //        .Where(x => x.ConversationId == request.ConversationId)
        //        .OrderBy(x => x.CreatedAt)
        //        .ToListAsync();

        //    // 2. User ka new question database mein save karo
        //    var userMessage = new ChatMessage
        //    {
        //        ConversationId = request.ConversationId,
        //        Role = "user",
        //        Content = request.Message,
        //        CreatedAt = DateTime.UtcNow
        //    };

        //    _db.ChatMessages.Add(userMessage);

        //    await _db.SaveChangesAsync();

        //    // 3. New question ko history mein add karo
        //    chatHistory.Add(userMessage);

        //    // 4. Complete conversation ko prompt mein convert karo
        //    var conversation = string.Join(
        //        "\n",
        //        chatHistory.Select(x => $"{x.Role}: {x.Content}")
        //    );

        //    // 5. Ollama request
        //    var requestBody = new
        //    {
        //        model = "llama3.2:3b",
        //        system = "You are a helpful assistant. Answer clearly and accurately.",
        //        prompt = conversation,
        //        stream = false,
        //        temperature = 0.2
        //    };

        //    // 6. Request ko JSON mein convert karo
        //    var json = JsonSerializer.Serialize(requestBody);

        //    var content = new StringContent(
        //        json,
        //        Encoding.UTF8,
        //        "application/json"
        //    );

        //    // 7. Ollama ko request bhejo
        //    var response = await _httpClient.PostAsync(
        //        "http://localhost:11434/api/generate",
        //        content
        //    );

        //    // 8. Ollama response read karo
        //    var result = await response.Content.ReadAsStringAsync();

        //    // 9. Ollama JSON ko C# object mein convert karo
        //    var ollamaResponse =
        //        JsonSerializer.Deserialize<OllamaResponse>(result);

        //    // 10. AI answer database mein save karo
        //    var assistantMessage = new ChatMessage
        //    {
        //        ConversationId = request.ConversationId,
        //        Role = "assistant",
        //        Content = ollamaResponse?.Response ?? "",
        //        CreatedAt = DateTime.UtcNow
        //    };

        //    _db.ChatMessages.Add(assistantMessage);

        //    await _db.SaveChangesAsync();

        //    // 11. API response
        //    return Ok(new
        //    {
        //        conversationId = request.ConversationId,
        //        answer = ollamaResponse?.Response
        //    });
        //}


        [HttpPost]
        public async Task<IActionResult> Chat([FromBody] ChatRequest request)
        {
            // ============================================
            // 1. Previous conversation history
            // ============================================

            var chatHistory = await _db.ChatMessages
                .Where(x => x.ConversationId == request.ConversationId)
                .OrderBy(x => x.CreatedAt)
                .ToListAsync();


            // ============================================
            // 2. Save user's new question
            // ============================================

            var userMessage = new ChatMessage
            {
                ConversationId = request.ConversationId,
                Role = "user",
                Content = request.Message,
                CreatedAt = DateTime.UtcNow
            };

            _db.ChatMessages.Add(userMessage);

            await _db.SaveChangesAsync();


            // ============================================
            // 3. Add new question to conversation history
            // ============================================

            chatHistory.Add(userMessage);


            // ============================================
            // 4. Convert question into embedding
            // ============================================

            var questionEmbedding =
                await _embeddingService.GetEmbedding(request.Message);


            // ============================================
            // 5. Get document chunks from database
            // ============================================

            var searchResults = await _qdrantService.SearchAsync(
    questionEmbedding,
    _topK,
    documentName: request.DocumentName);
            //var documents = await _db.DocumentChunks
            //    .Select(x => new
            //    {
            //        x.Content,
            //        x.EmbeddingJson,
            //        x.DocumentName,
            //        x.PageNumber
            //    })
            //    .ToListAsync();


            // ============================================
            // 6. Calculate similarity
            // ============================================

            var similarityResults = searchResults
    .Where(x => x.Score >= 0.50)
    .Select(x => new
    {
        Content = x.Payload["content"].StringValue,
        DocumentName = x.Payload["documentName"].StringValue,
        PageNumber = (int)x.Payload["pageNumber"].IntegerValue,
        Similarity = (double)x.Score
    })
    .ToList();

            //var similarityResults = documents
            //    .Select(document =>
            //    {
            //        var documentEmbedding =
            //            JsonSerializer.Deserialize<List<double>>(
            //                document.EmbeddingJson);

            //        if (documentEmbedding == null)
            //        {
            //            return null;
            //        }

            //        var similarity =
            //            CosineSimilarity(
            //                questionEmbedding,
            //                documentEmbedding);

            //        return new
            //        {
            //            document.Content,
            //            document.DocumentName,
            //            document.PageNumber,
            //            Similarity = similarity
            //        };
            //    })
            //    .Where(x => x != null)
            //    .Where(x => x.Similarity >= 0.50)
            //    .OrderByDescending(x => x.Similarity)
            //    .Take(3)
            //    .ToList();


            // ============================================
            // 7. No relevant document found
            // ============================================

            if (!similarityResults.Any())
            {
                var noAnswer =
                    "I could not find the answer in the provided document.";

                var assistantMessageRole = new ChatMessage
                {
                    ConversationId = request.ConversationId,
                    Role = "assistant",
                    Content = noAnswer,
                    CreatedAt = DateTime.UtcNow
                };

                _db.ChatMessages.Add(assistantMessageRole);

                await _db.SaveChangesAsync();

                return Ok(new
                {
                    conversationId = request.ConversationId,
                    answer = noAnswer,
                    sources = new List<object>()
                });
            }
            //if (!similarityResults.Any())
            //{
            //    var noAnswer =
            //        "I could not find the answer in the provided document.";

            //    var noAnswerMessage = new ChatMessage
            //    {
            //        ConversationId = request.ConversationId,
            //        Role = "assistant",
            //        Content = noAnswer,
            //        CreatedAt = DateTime.UtcNow
            //    };

            //    _db.ChatMessages.Add(noAnswerMessage);

            //    await _db.SaveChangesAsync();

            //    return Ok(new
            //    {
            //        conversationId = request.ConversationId,
            //        answer = noAnswer,
            //        sources = new List<object>()
            //    });
            //}

            //var assistantMessage = new ChatMessage
            //{
            //    ConversationId = request.ConversationId,
            //    Role = "assistant",
            //    Content = answer,
            //    CreatedAt = DateTime.UtcNow
            //};

            //_db.ChatMessages.Add(assistantMessage);

            //await _db.SaveChangesAsync();

            // ============================================
            // 8. Create context from retrieved chunks
            // ============================================

            var context = string.Join(
                "\n\n---\n\n",
                similarityResults.Select(x =>
                    $"""
            Document: {x.DocumentName}
            Page: {x.PageNumber}

            Content:
            {x.Content}
            """
                )
            );


            // ============================================
            // 9. Create conversation history
            // ============================================

            var conversation = string.Join(
                "\n",
                chatHistory.Select(x =>
                    $"{x.Role}: {x.Content}"
                )
            );


            // ============================================
            // 10. Create RAG prompt
            // ============================================

            var prompt = $"""
    You are a helpful insurance assistant.

    Answer the user's question using ONLY the information
    provided in the context below.

    If the answer is not available in the context,
    say:

    "I could not find the answer in the provided document."

    Context:
    {context}

    Conversation:
    {conversation}

    Current Question:
    {request.Message}

    Answer:
    """;


            // ============================================
            // 11. Ollama request
            // ============================================

            var requestBody = new
            {
                model = _chatModel/*"llama3.2:3b"*/,

                system =
                    "You are a helpful insurance assistant. " +
                    "Answer clearly and accurately.",

                prompt = prompt,

                stream = false,

               _temperature //temperature = 0.2
            };


            // ============================================
            // 12. Convert request to JSON
            // ============================================

            var json =
                JsonSerializer.Serialize(requestBody);

            var content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json"
            );


            // ============================================
            // 13. Send request to Ollama
            // ============================================

            var response = await _httpClient.PostAsync(
                $"{_baseUrl}/api/generate",
                content
            );


            // ============================================
            // 14. Check Ollama response
            // ============================================

            response.EnsureSuccessStatusCode();


            // ============================================
            // 15. Read Ollama response
            // ============================================

            var result =
                await response.Content.ReadAsStringAsync();


            // ============================================
            // 16. Convert JSON to C# object
            // ============================================

            var ollamaResponse =
                JsonSerializer.Deserialize<OllamaResponse>(
                    result);


            var answer =
                ollamaResponse?.Response
                ?? "No answer received.";


            // ============================================
            // 17. Save AI answer
            // ============================================

            var assistantMessage = new ChatMessage
            {
                ConversationId = request.ConversationId,
                Role = "assistant",
                Content = answer,
                CreatedAt = DateTime.UtcNow
            };

            _db.ChatMessages.Add(assistantMessage);

            await _db.SaveChangesAsync();


            // ============================================
            // 18. Return answer + source information
            // ============================================

            return Ok(new
            {
                conversationId = request.ConversationId,

                question = request.Message,

                answer = answer,

                sources = similarityResults.Select(x => new
                {
                    documentName = x.DocumentName,

                    pageNumber = x.PageNumber,

                    similarity = x.Similarity
                })
            });
        }


        [HttpPost("clear")]
        public async Task<IActionResult> ClearChat(
            [FromQuery] string conversationId)
        {
            // Conversation ke saare messages find karo
            var messages = await _db.ChatMessages
                .Where(x => x.ConversationId == conversationId)
                .ToListAsync();

            // Messages delete karo
            _db.ChatMessages.RemoveRange(messages);

            // Database mein delete apply karo
            await _db.SaveChangesAsync();

            return Ok(new
            {
                message = "Conversation cleared successfully."
            });
        }


        [HttpDelete("delete")]
        public async Task<IActionResult> DeleteDocument([FromQuery] string documentName)
        {
            // Step 1: SQL se delete karo
            var chunksToDelete = await _db.DocumentChunks
                .Where(x => x.DocumentName == documentName)
                .ToListAsync();

            if (!chunksToDelete.Any())
            {
                return NotFound(new { message = "Document not found." });
            }

            _db.DocumentChunks.RemoveRange(chunksToDelete);
            await _db.SaveChangesAsync();

            // Step 2: Qdrant se bhi delete karo
            await _qdrantService.DeleteByDocumentNameAsync(documentName);

            return Ok(new
            {
                message = $"Document '{documentName}' deleted successfully.",
                deletedChunks = chunksToDelete.Count
            });
        }

    }
}

