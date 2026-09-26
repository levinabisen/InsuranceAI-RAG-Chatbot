using System.Text.Json.Serialization;

namespace InsuranceAI.Api.Model
{
    public class OllamaResponse
    {
        [JsonPropertyName("model")]
        public string? Model { get; set; }

        [JsonPropertyName("response")]
        public string? Response { get; set; }
    }
}
