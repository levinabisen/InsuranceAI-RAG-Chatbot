using InsuranceAI.Api.Model;

namespace InsuranceAI.Api.Services
{
    public interface IChunkService
    {
        List<DocumentChunk> CreateChunks(
            string text,
            string documentName,
            int chunkSize = 1000);
    }
}
