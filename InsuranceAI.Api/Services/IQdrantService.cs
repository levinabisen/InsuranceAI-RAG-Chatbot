using Qdrant.Client.Grpc;

namespace InsuranceAI.Api.Services
{
    public interface IQdrantService
    {
        Task EnsureCollectionExistsAsync();
        Task UpsertChunkAsync(int id, string content, List<double> embedding, string documentName, int pageNumber);
        Task<List<ScoredPoint>> SearchAsync(List<double> queryEmbedding, int topK = 3);

        Task DeleteByDocumentNameAsync(string documentName);   // NAYA

        Task<List<ScoredPoint>> SearchAsync(List<double> queryEmbedding, int topK = 3, string? documentName = null);

    }
}
