using System.Text.Json.Serialization;

namespace InsuranceAI.Api.Model
{
    public class EmbeddingRequest
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        
    }
}
