namespace Cursus.BLL.Services;

/// <summary>
/// Small boundary around the OpenAI SDK so advisor behavior can be tested
/// without making external API requests.
/// </summary>
public interface IOpenAiChatClient
{
    bool IsConfigured { get; }

    Task<string?> CompleteAsync(
        string systemPrompt,
        string userMessage,
        CancellationToken cancellationToken = default);
}
