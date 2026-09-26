namespace InsuranceAI.Api.Model
{
    public class ChatMessage
    {
        public int Id { get; set; }

        public string ConversationId { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }
}
