using Cursus.Domain.DTOs;
using Cursus.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Cursus.BLL.Services;

/// <summary>
/// Service for AI Advisor chat functionality powered by an OpenAI-compatible provider.
/// Provides context-aware academic guidance based on student profile data.
/// </summary>
public class AiAdvisorService : IAiAdvisorService
{
    private const int MaxHistoryMessages = 12;
    private const int MaxHistoryMessageLength = 2000;

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
        CancellationToken cancellationToken = default,
        IEnumerable<AiAdvisorMessageDto>? conversationHistory = null)
    {
        ArgumentNullException.ThrowIfNull(studentContext);

        if (string.IsNullOrWhiteSpace(userMessage))
            throw new ArgumentException("An advisor message is required.", nameof(userMessage));

        if (!_chatClient.IsConfigured)
        {
            _logger.LogWarning("AI advisor request rejected because the OpenAI-compatible provider is not configured.");
            return AiAdvisorResponseDto.Failure("openai_not_configured", UnavailableMessage);
        }

        try
        {
            var systemPrompt = AiAdvisorPromptBuilder.BuildSystemPrompt(studentContext);
            var safeHistory = NormalizeHistory(conversationHistory);
            var responseText = await _chatClient.CompleteAsync(
                systemPrompt,
                userMessage.Trim(),
                cancellationToken,
                safeHistory);
            var normalizedResponseText = NormalizeProviderResponse(responseText);

            if (string.IsNullOrWhiteSpace(normalizedResponseText))
            {
                _logger.LogWarning("The OpenAI-compatible provider returned an empty AI advisor response.");
                return AiAdvisorResponseDto.Failure(
                    "openai_empty_response",
                    UnavailableMessage);
            }

            return AiAdvisorResponseDto.Success(normalizedResponseText);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "The OpenAI-compatible provider failed to process an AI advisor request.");
            return AiAdvisorResponseDto.Failure(
                "openai_request_failed",
                UnavailableMessage);
        }
    }

    private static IReadOnlyList<AiAdvisorMessageDto> NormalizeHistory(
        IEnumerable<AiAdvisorMessageDto>? conversationHistory)
    {
        if (conversationHistory is null)
            return [];

        var safeMessages = conversationHistory
            .Where(message => message is not null)
            .Select(message => new AiAdvisorMessageDto
            {
                Role = NormalizeRole(message.Role),
                Content = TruncateHistoryContent(message.Content)
            })
            .Where(message =>
                !string.IsNullOrWhiteSpace(message.Role) &&
                !string.IsNullOrWhiteSpace(message.Content))
            .ToList();

        return safeMessages.Count <= MaxHistoryMessages
            ? safeMessages
            : safeMessages.Skip(safeMessages.Count - MaxHistoryMessages).ToList();
    }

    private static string NormalizeRole(string? role) =>
        role?.Trim().ToLowerInvariant() switch
        {
            "user" or "student" => "user",
            "assistant" or "ai" or "model" => "assistant",
            _ => string.Empty
        };

    private static string TruncateHistoryContent(string? content)
    {
        var trimmed = content?.Trim() ?? string.Empty;
        return trimmed.Length <= MaxHistoryMessageLength
            ? trimmed
            : trimmed[..MaxHistoryMessageLength];
    }

    private static string? NormalizeProviderResponse(string? responseText)
    {
        var trimmed = responseText?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            return null;

        if (trimmed[0] is not ('{' or '['))
            return trimmed;

        try
        {
            using var document = JsonDocument.Parse(trimmed);
            var extracted = new List<string>();
            CollectTextValues(document.RootElement, extracted);

            if (extracted.Count > 0)
            {
                return string.Join(
                    Environment.NewLine + Environment.NewLine,
                    extracted.Select(value => value.Trim()).Where(value => value.Length > 0));
            }
        }
        catch (JsonException)
        {
            return trimmed;
        }

        return trimmed;
    }

    private static void CollectTextValues(JsonElement element, List<string> values)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    if ((property.NameEquals("text") ||
                         property.NameEquals("content") ||
                         property.NameEquals("reply")) &&
                        property.Value.ValueKind == JsonValueKind.String)
                    {
                        var value = property.Value.GetString();
                        if (!string.IsNullOrWhiteSpace(value))
                            values.Add(value);

                        continue;
                    }

                    CollectTextValues(property.Value, values);
                }
                break;

            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                    CollectTextValues(item, values);
                break;
        }
    }
}
