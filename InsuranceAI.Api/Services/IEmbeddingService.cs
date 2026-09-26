namespace InsuranceAI.Api.Services
{
    public interface IEmbeddingService
    {
        Task<List<double>> GetEmbedding(string text);
    }
}
