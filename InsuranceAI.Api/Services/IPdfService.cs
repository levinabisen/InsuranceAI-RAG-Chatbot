namespace InsuranceAI.Api.Services
{
    public interface IPdfService
    {
        string ExtractText(string filePath);

        //List<string> CreateChunks(
        //    string text,
        //    int chunkSize = 500
        //);

        

        List<string> CreateChunks(
            string text,
            int chunkSize = 500,
            int overlap = 100);
    }
}
