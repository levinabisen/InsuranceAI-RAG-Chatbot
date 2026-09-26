namespace InsuranceAI.Api.Services
{
    public interface IAiService
    {
        Task<string> GetResponseAsync(string prompt);
    }
}
