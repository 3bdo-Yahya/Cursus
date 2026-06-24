namespace Cursus.Domain.Interfaces.Services
{
    public interface IGeminiService
    {
        Task<string> AskGeminiAsync(string prompt);
    }
}