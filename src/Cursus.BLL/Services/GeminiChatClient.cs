using System.Threading;
using System.Threading.Tasks;
using GenerativeAI;
using Microsoft.Extensions.Options;
using Cursus.BLL.Options;

namespace Cursus.BLL.Services
{
    public sealed class GeminiChatClient : IGeminiChatClient
    {
        private readonly GenerativeModel? _model;
        private readonly GeminiOptions _options;

        public GeminiChatClient(IOptions<GeminiOptions> options)
        {
            _options = options.Value;

            if (!string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                var modelName = string.IsNullOrWhiteSpace(_options.Model)
                    ? "models/gemini-2.5-flash"
                    : _options.Model.Trim();

                _model = new GenerativeModel(
                    apiKey: _options.ApiKey.Trim(),
                    model: modelName
                );
            }
        }

        public bool IsConfigured => _model is not null;

        public async Task<string?> GenerateContentAsync(string prompt, CancellationToken cancellationToken = default)
        {
            if (_model == null)
                return null;

            // Call the SDK to generate the content
            var result = await _model.GenerateContentAsync(prompt);
            return result?.Text();
        }
    }
}
