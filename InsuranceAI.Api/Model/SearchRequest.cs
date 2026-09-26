using System.Text.Json.Serialization;

namespace InsuranceAI.Api.Model
{
    public class SearchRequest
    {
        [JsonPropertyName("query")]
        public string Query { get; set; } = string.Empty;
    }
}
