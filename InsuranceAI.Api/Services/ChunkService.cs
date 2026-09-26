using InsuranceAI.Api.Model;

namespace InsuranceAI.Api.Services
{
    public class ChunkService : IChunkService
    {
        public List<DocumentChunk> CreateChunks(
            string text,
            string documentName,
            int chunkSize = 1000)
        {
            var chunks = new List<DocumentChunk>();

            if (string.IsNullOrWhiteSpace(text))
                return chunks;

            int chunkIndex = 0;

            for (int i = 0; i < text.Length; i += chunkSize)
            {
                int length = Math.Min(
                    chunkSize,
                    text.Length - i);

                string content = text
                    .Substring(i, length)
                    .Trim();

                //if (!string.IsNullOrWhiteSpace(content))
                //{
                //    chunks.Add(new DocumentChunk
                //    {
                //        Content = content,
                //        ChunkIndex = chunkIndex,
                //        DocumentName = documentName
                //    });

                //    chunkIndex++;
                //}
            }

            return chunks;
        }
    }
}
