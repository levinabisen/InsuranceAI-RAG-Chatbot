using Qdrant.Client.Grpc;
using Qdrant.Client;

namespace InsuranceAI.Api.Services
{
    public class QdrantService: IQdrantService
    {
        
    
        private readonly QdrantClient _client;
        private const string CollectionName = "document_chunks";
        private const int VectorSize = 768;

        public QdrantService()
        {
            _client = new QdrantClient("localhost", 6334);
        }

        public async Task EnsureCollectionExistsAsync()
        {
            var collections = await _client.ListCollectionsAsync();

            if (!collections.Contains(CollectionName))
            {
                await _client.CreateCollectionAsync(
                    collectionName: CollectionName,
                    vectorsConfig: new VectorParams
                    {
                        Size = VectorSize,
                        Distance = Distance.Cosine
                    }
                );
            }
        }

        public async Task UpsertChunkAsync(
    int id,
    string content,
    List<double> embedding,
    string documentName,
    int pageNumber)
        {
            // double list ko float array me convert karo
            float[] embeddingArray = embedding.Select(x => (float)x).ToArray();

            var point = new PointStruct
            {
                Id = (ulong)id,
                Vectors = embeddingArray,
                Payload =
        {
            ["content"] = content,
            ["documentName"] = documentName,
            ["pageNumber"] = pageNumber
        }
            };

            await _client.UpsertAsync(CollectionName, new List<PointStruct> { point });
        }


        public async Task<List<ScoredPoint>> SearchAsync(List<double> queryEmbedding, int topK = 3)
        {
            // Query embedding ko float array me convert karo
            float[] queryVector = queryEmbedding.Select(x => (float)x).ToArray();

            // Qdrant se search karo - top K similar points
            var results = await _client.SearchAsync(
                collectionName: CollectionName,
                vector: queryVector,
                limit: (ulong)topK
            );

            return results.ToList();
        }


        public async Task DeleteByDocumentNameAsync(string documentName)
        {
            await _client.DeleteAsync(
                collectionName: CollectionName,
                filter: new Filter
                {
                    Must =
                    {
                new Condition
                {
                    Field = new FieldCondition
                    {
                        Key = "documentName",
                        Match = new Match { Text = documentName }
                    }
                }
                    }
                }
            );
        }

        public async Task<List<ScoredPoint>> SearchAsync(
    List<double> queryEmbedding,
    int topK = 3,
    string? documentName = null)
        {
            float[] queryVector = queryEmbedding.Select(x => (float)x).ToArray();

            Filter? filter = null;

            if (!string.IsNullOrEmpty(documentName))
            {
                filter = new Filter
                {
                    Must =
            {
                new Condition
                {
                    Field = new FieldCondition
                    {
                        Key = "documentName",
                        Match = new Match { Text = documentName }
                    }
                }
            }
                };
            }

            var results = await _client.SearchAsync(
                collectionName: CollectionName,
                vector: queryVector,
                filter: filter,
                limit: (ulong)topK
            );

            return results.ToList();
        }
    }
}
