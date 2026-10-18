using InsuranceAI.Api.Model;
using Microsoft.Extensions.Options;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace InsuranceAI.Api.Services
{
    public class QdrantService: IQdrantService
    {
        
    
        private readonly QdrantClient _client;
        //private const string CollectionName = "document_chunks";
        //private const int VectorSize = 768;
        private readonly string _collectionName;
        private readonly int _vectorSize;


        public QdrantService(IOptions<QdrantOptions> options)
        {
           // _client = new QdrantClient("localhost", 6334);
            var config = options.Value;
            _collectionName = config.CollectionName;
            _vectorSize = config.VectorSize;
            _client = new QdrantClient(config.Host, config.Port);
        }

        public async Task EnsureCollectionExistsAsync()
        {
            var collections = await _client.ListCollectionsAsync();

            if (!collections.Contains(_collectionName))
            {
                await _client.CreateCollectionAsync(
                    collectionName: _collectionName,
                    vectorsConfig: new VectorParams
                    {
                        //Size = _vectorSize,
                        Size = ((ulong)_vectorSize),
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

            await _client.UpsertAsync(_collectionName, new List<PointStruct> { point });
        }


        public async Task<List<ScoredPoint>> SearchAsync(List<double> queryEmbedding, int topK = 3)
        {
            // Query embedding ko float array me convert karo
            float[] queryVector = queryEmbedding.Select(x => (float)x).ToArray();

            // Qdrant se search karo - top K similar points
            var results = await _client.SearchAsync(
                collectionName: _collectionName,
                vector: queryVector,
                limit: (ulong)topK
            );

            return results.ToList();
        }


        public async Task DeleteByDocumentNameAsync(string documentName)
        {
            await _client.DeleteAsync(
                collectionName: _collectionName,
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
                collectionName: _collectionName,
                vector: queryVector,
                filter: filter,
                limit: (ulong)topK
            );

            return results.ToList();
        }
    }
}
