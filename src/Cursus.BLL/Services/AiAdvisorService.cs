using Cursus.Domain.DTOs;
using Cursus.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace Cursus.BLL.Services;

/// <summary>
/// Service for AI Advisor chat functionality powered by OpenAI.
/// Provides context-aware academic guidance based on student profile data.
/// </summary>
public class AiAdvisorService : IAiAdvisorService
{
    private const string UnavailableMessage =
        "The AI advisor is temporarily unavailable. Please try again later.";

    private readonly IOpenAiChatClient _chatClient;
    private readonly ILogger<AiAdvisorService> _logger;

    public AiAdvisorService(
        IOpenAiChatClient chatClient,
        ILogger<AiAdvisorService> logger)
    {
        _chatClient = chatClient;
        _logger = logger;
    }

    /// <summary>
    /// Gets an AI advisor response grounded in the supplied student context.
    /// </summary>
    public async Task<AiAdvisorResponseDto> GetAdvisorResponseAsync(
        AiAdvisorContextDto studentContext,
        string userMessage,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(studentContext);

        if (string.IsNullOrWhiteSpace(userMessage))
            throw new ArgumentException("An advisor message is required.", nameof(userMessage));

        if (!_chatClient.IsConfigured)
        {
            _logger.LogWarning("AI advisor request rejected because OpenAI is not configured.");
            return AiAdvisorResponseDto.Failure("openai_not_configured", UnavailableMessage);
        }

        try
        {
            var systemPrompt = AiAdvisorPromptBuilder.BuildSystemPrompt(studentContext);
            var responseText = await _chatClient.CompleteAsync(
                systemPrompt,
                userMessage.Trim(),
                cancellationToken);

            if (string.IsNullOrWhiteSpace(responseText))
            {
                _logger.LogWarning("OpenAI returned an empty AI advisor response.");
                return AiAdvisorResponseDto.Failure(
                    "openai_empty_response",
                    UnavailableMessage);
            }

            return AiAdvisorResponseDto.Success(responseText);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenAI failed to process an AI advisor request.");
            return AiAdvisorResponseDto.Failure(
                "openai_request_failed",
                UnavailableMessage);
        }
    }
}
