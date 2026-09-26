using System.Text.Json.Serialization;

namespace InsuranceAI.Api.Model
{
    public class EmbeddingResponse
    {
        [JsonPropertyName("embedding")]
        public List<double> Embedding { get; set; } = new();
    }
}
