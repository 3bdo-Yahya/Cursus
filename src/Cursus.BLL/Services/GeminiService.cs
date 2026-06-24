using GenerativeAI;

namespace Cursus.BLL.Services
{
    public class GeminiService : IGeminiService
    {
        private readonly GenerativeModel _model;

        public GeminiService(string apiKey)
        {
            _model = new GenerativeModel(
                apiKey: apiKey,
                model: "models/gemini-2.5-flash"
            );
        }
        public async Task<string> AskGeminiAsync(string prompt)
        {
            var result = await _model.GenerateContentAsync(prompt);
            return result?.Text() ?? "No response";
        }
    }
}