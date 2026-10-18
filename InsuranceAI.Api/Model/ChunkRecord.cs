using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.Qdrant;
namespace InsuranceAI.Api.Model
{
    public class ChunkRecord
    {


        [VectorStoreKey]
        public ulong Id { get; set; }

        [VectorStoreData]
        public string Content { get; set; } = string.Empty;

        //[VectorStoreRecordData]
        [VectorStoreData]
        public string DocumentName { get; set; } = string.Empty;

        [VectorStoreData]
        public int PageNumber { get; set; }

        [VectorStoreData]

        //[VectorStoreRecordVector(768)
        public ReadOnlyMemory<float> Embedding { get; set; }
    }
}
