namespace InsuranceAI.Api.Model
{
    public class QdrantUpsertRequest
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public string DocumentName { get; set; } = string.Empty;
        public int PageNumber { get; set; }
    }
}
