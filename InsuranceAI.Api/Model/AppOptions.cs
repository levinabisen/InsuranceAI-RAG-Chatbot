namespace InsuranceAI.Api.Model
{
    public class QdrantOptions
    {
        public string Host { get; set; } = "localhost";
        public int Port { get; set; } = 6334;
        public string CollectionName { get; set; } = string.Empty;
        public int VectorSize { get; set; }
    }

    public class OllamaOptions
    {
        public string BaseUrl { get; set; } = string.Empty;
        public string ChatModel { get; set; } = string.Empty;
        public double Temperature { get; set; }
    }

    public class RagOptions
    {
        public int TopK { get; set; }
        public double SimilarityThreshold { get; set; }
    }
}