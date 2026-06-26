using System;
using System.Threading;
using System.Threading.Tasks;
using Cursus.Domain.DTOs;
using Cursus.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace Cursus.BLL.Services
{
    public class GeminiService : IGeminiService
    {
        private readonly IGeminiChatClient _chatClient;
        private readonly ILogger<GeminiService> _logger;

        public GeminiService(IGeminiChatClient chatClient, ILogger<GeminiService> logger)
        {
            _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string> AskGeminiAsync(
            GraduationAuditDto audit,
            ChatRequestDto request,
            CancellationToken cancellationToken = default)
        {
            if (audit == null) throw new ArgumentNullException(nameof(audit));
            if (request == null) throw new ArgumentNullException(nameof(request));

            if (!_chatClient.IsConfigured)
            {
                _logger.LogWarning("GeminiService.AskGeminiAsync called but GeminiChatClient is not configured.");
                return "The AI advisor is temporarily unconfigured. Please contact your administrator.";
            }

            try
            {
                var prompt = GeminiPromptBuilder.BuildPrompt(audit, request);
                var reply = await _chatClient.GenerateContentAsync(prompt, cancellationToken);

                return string.IsNullOrWhiteSpace(reply) ? "No response received from the advisor." : reply.Trim();
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("GeminiService.AskGeminiAsync request was canceled.");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in GeminiService.AskGeminiAsync while generating content.");
                return "The AI advisor is temporarily unavailable. Please try again later.";
            }
        }
    }
}