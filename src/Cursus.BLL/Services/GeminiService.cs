using System;
using System.Threading;
using System.Threading.Tasks;
using Cursus.Domain.DTOs;
using Cursus.Domain.Interfaces.Services;

namespace Cursus.BLL.Services
{
    public class GeminiService : IGeminiService
    {
        private readonly IGeminiChatClient _chatClient;

        public GeminiService(IGeminiChatClient chatClient)
        {
            _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
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
                throw;
            }
            catch (Exception ex)
            {
                // In production, we'd log the exception using ILogger
                return "The AI advisor is temporarily unavailable. Please try again later.";
            }
        }
    }
}