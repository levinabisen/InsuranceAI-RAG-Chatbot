using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;

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
    }
}
