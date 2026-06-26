using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GenerativeAI;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Cursus.BLL.Options;

namespace Cursus.BLL.Services
{
    public sealed class GeminiChatClient : IGeminiChatClient
    {
        private readonly IOptionsMonitor<GeminiOptions> _optionsMonitor;
        private readonly ILogger<GeminiChatClient> _logger;

        public GeminiChatClient(IOptionsMonitor<GeminiOptions> optionsMonitor, ILogger<GeminiChatClient> logger)
        {
            _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public bool IsConfigured => !string.IsNullOrWhiteSpace(_optionsMonitor.CurrentValue.ApiKey);

        public async Task<string?> GenerateContentAsync(string prompt, CancellationToken cancellationToken = default)
        {
            var options = _optionsMonitor.CurrentValue;
            if (string.IsNullOrWhiteSpace(options.ApiKey))
            {
                _logger.LogWarning("GeminiChatClient.GenerateContentAsync called but no ApiKey is configured.");
                return null;
            }

            // Support both a single API key and a comma-separated list of fallback API keys
            var keys = options.ApiKey
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();

            if (keys.Count == 0)
            {
                _logger.LogWarning("GeminiChatClient.GenerateContentAsync: ApiKey resolved to an empty list after splitting.");
                return null;
            }

            var modelName = string.IsNullOrWhiteSpace(options.Model)
                ? "models/gemini-2.5-flash"
                : options.Model.Trim();

            Exception? lastException = null;

            for (int i = 0; i < keys.Count; i++)
            {
                var key = keys[i];
                var obfuscatedKey = ObfuscateKey(key);

                try
                {
                    if (keys.Count > 1)
                    {
                        _logger.LogInformation("Attempting Generative AI request using API Key {Index}/{Total} ({ObfuscatedKey})", i + 1, keys.Count, obfuscatedKey);
                    }

                    var model = new GenerativeModel(
                        apiKey: key,
                        model: modelName
                    );

                    // Call the SDK to generate the content
                    var result = await model.GenerateContentAsync(prompt);
                    return result?.Text();
                }
                catch (Exception ex) when (IsRateLimitOrQuotaError(ex))
                {
                    lastException = ex;
                    _logger.LogWarning(ex, "API Key {Index}/{Total} ({ObfuscatedKey}) encountered a rate limit or quota exhaustion. Swapping to next key.", i + 1, keys.Count, obfuscatedKey);
                    
                    if (i == keys.Count - 1)
                    {
                        _logger.LogError("All configured Gemini API keys in the rotation pool have been exhausted.");
                    }
                }
                catch (Exception ex)
                {
                    // For non-rate-limit/non-quota errors, throw immediately (e.g. network failure, bad prompt)
                    _logger.LogError(ex, "Gemini request failed with an unrecoverable error using Key {Index}/{Total}.", i + 1, keys.Count);
                    throw;
                }
            }

            if (lastException != null)
            {
                throw lastException;
            }

            return null;
        }

        private static bool IsRateLimitOrQuotaError(Exception ex)
        {
            var exceptionString = ex.ToString();
            return exceptionString.Contains("429") ||
                   exceptionString.Contains("RESOURCE_EXHAUSTED") ||
                   exceptionString.Contains("quota") ||
                   exceptionString.Contains("rate limit") ||
                   exceptionString.Contains("Too Many Requests");
        }

        private static string ObfuscateKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return "empty";
            if (key.Length <= 8) return "***";
            return $"{key[..4]}...{key[^4..]}";
        }
    }
}

