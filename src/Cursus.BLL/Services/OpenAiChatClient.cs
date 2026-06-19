using Cursus.BLL.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace Cursus.BLL.Services;

/// <summary>
/// OpenAI .NET SDK wrapper used by the AI advisor service.
/// </summary>
public sealed class OpenAiChatClient : IOpenAiChatClient
{
    private readonly ChatClient? _client;
    private readonly OpenAiOptions _options;
    private readonly ILogger<OpenAiChatClient> _logger;

    public OpenAiChatClient(
        IOptions<OpenAiOptions> options,
        ILogger<OpenAiChatClient> logger)
    {
        _options = options.Value;
        _logger = logger;

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            _logger.LogWarning(
                "OpenAI is not configured. Set {ConfigurationKey} using user secrets or an environment variable.",
                $"{OpenAiOptions.SectionName}:ApiKey");
            return;
        }

        var model = string.IsNullOrWhiteSpace(_options.Model)
            ? "gpt-4o-mini"
            : _options.Model.Trim();

        _client = new ChatClient(model, _options.ApiKey.Trim());
    }

    public bool IsConfigured => _client is not null;

    public async Task<string?> CompleteAsync(
        string systemPrompt,
        string userMessage,
        CancellationToken cancellationToken = default)
    {
        if (_client is null)
            return null;

        var messages = new ChatMessage[]
        {
            new SystemChatMessage(systemPrompt),
            new UserChatMessage(userMessage)
        };

        var completionOptions = new ChatCompletionOptions
        {
            Temperature = Math.Clamp(_options.Temperature, 0f, 2f),
            TopP = Math.Clamp(_options.TopP, 0f, 1f),
            MaxOutputTokenCount = Math.Max(1, _options.MaxOutputTokenCount)
        };

        ChatCompletion completion = await _client.CompleteChatAsync(
            messages,
            completionOptions,
            cancellationToken);

        var responseText = string.Join(
            Environment.NewLine,
            completion.Content
                .Where(part => !string.IsNullOrWhiteSpace(part.Text))
                .Select(part => part.Text));

        return string.IsNullOrWhiteSpace(responseText)
            ? null
            : responseText.Trim();
    }
}
