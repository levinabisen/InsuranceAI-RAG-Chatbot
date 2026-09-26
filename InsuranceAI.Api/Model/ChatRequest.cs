using System.Text.Json.Serialization;

namespace InsuranceAI.Api.Model;



public class ChatRequest
{
    public string ConversationId { get; set; } = string.Empty;

    // public string Prompt { get; set; } = string.Empty;
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    public string DocumentName { get; set; } = string.Empty;


}