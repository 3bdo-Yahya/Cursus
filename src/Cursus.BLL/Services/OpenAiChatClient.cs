using Cursus.BLL.Options;
using Cursus.Domain.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;

namespace Cursus.BLL.Services;

/// <summary>
/// OpenAI-compatible .NET SDK wrapper used by the AI advisor service.
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
                "The AI advisor provider is not configured. Set {ConfigurationKey} using user secrets or an environment variable.",
                $"{OpenAiOptions.SectionName}:ApiKey");
            return;
        }

        var model = string.IsNullOrWhiteSpace(_options.Model)
            ? OpenAiOptions.DefaultModel
            : _options.Model.Trim();

        _client = CreateChatClient(model);
    }

    public bool IsConfigured => _client is not null;

    public async Task<string?> CompleteAsync(
        string systemPrompt,
        string userMessage,
        CancellationToken cancellationToken = default,
        IReadOnlyList<AiAdvisorMessageDto>? conversationHistory = null)
    {
        if (_client is null)
            return null;

        var messages = BuildMessages(systemPrompt, userMessage, conversationHistory);

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

    private ChatClient CreateChatClient(string model)
    {
        var credential = new ApiKeyCredential(_options.ApiKey.Trim());

        if (string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            return new ChatClient(model, credential);
        }

        if (!Uri.TryCreate(_options.BaseUrl.Trim(), UriKind.Absolute, out var endpoint))
        {
            throw new InvalidOperationException("OpenAi:BaseUrl must be a valid absolute URL.");
        }

        return new ChatClient(
            model,
            credential,
            new OpenAIClientOptions
            {
                Endpoint = endpoint
            });
    }

    private static List<ChatMessage> BuildMessages(
        string systemPrompt,
        string userMessage,
        IReadOnlyList<AiAdvisorMessageDto>? conversationHistory)
    {
        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(systemPrompt)
        };

        foreach (var message in conversationHistory ?? [])
        {
            if (string.Equals(message.Role, "user", StringComparison.OrdinalIgnoreCase))
            {
                messages.Add(new UserChatMessage(message.Content));
                continue;
            }

            if (string.Equals(message.Role, "assistant", StringComparison.OrdinalIgnoreCase))
            {
                messages.Add(new AssistantChatMessage(message.Content));
            }
        }

        messages.Add(new UserChatMessage(userMessage));
        return messages;
    }
}
