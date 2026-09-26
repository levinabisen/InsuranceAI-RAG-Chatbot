namespace InsuranceAI.Api.Model
{
    public class DocumentChunk
    {
        public int Id { get; set; }

        public string Content { get; set; } = string.Empty;

        public string EmbeddingJson { get; set; } = string.Empty;
        //for RAG me Metadata + Source Tracking
        public string DocumentName { get; set; } = string.Empty;

        public int PageNumber { get; set; }
        // two field add for pdf chunking
        //public int ChunkIndex { get; set; }

        //public string DocumentName { get; set; } = string.Empty;
    }
}
