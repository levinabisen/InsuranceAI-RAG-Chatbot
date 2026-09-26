using InsuranceAI.Api.Services;

namespace InsuranceAI.Api
{
    public class AiService : IAiService
    {
        public Task<string> GetResponseAsync(string prompt)
        {
            return Task.FromResult(
                $"AI response will be generated for: {prompt}"
            );
        }
    }
}
