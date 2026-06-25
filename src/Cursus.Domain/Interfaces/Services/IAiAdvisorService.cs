using Cursus.Domain.DTOs;

namespace Cursus.Domain.Interfaces.Services;

/// <summary>
/// Service interface for AI Advisor chat functionality.
/// Provides context-aware responses powered by an OpenAI-compatible provider.
/// </summary>
public interface IAiAdvisorService
{
    /// <summary>
    /// Gets an AI advisor response for a student's query and academic context.
    /// </summary>
    /// <param name="studentContext">The academic profile used to ground the response</param>
    /// <param name="userMessage">The student's message/question</param>
    /// <param name="cancellationToken">Token used to cancel the provider request</param>
    /// <param name="conversationHistory">Recent user/assistant turns used only for conversational continuity</param>
    /// <returns>A typed success or failure result for the caller</returns>
    Task<AiAdvisorResponseDto> GetAdvisorResponseAsync(
        AiAdvisorContextDto studentContext,
        string userMessage,
        CancellationToken cancellationToken = default,
        IEnumerable<AiAdvisorMessageDto>? conversationHistory = null);
}
