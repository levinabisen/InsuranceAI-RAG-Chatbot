using System.Text.Json.Serialization;

namespace InsuranceAI.Api
{
    public class OllamaResponse
    {
        [JsonPropertyName("response")]
        public string Response { get; set; } = string.Empty;
        
    }
}
