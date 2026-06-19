using Cursus.BLL.Services;
using Cursus.Domain.DTOs;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cursus.BLL.Tests;

public sealed class AiAdvisorServiceTests
{
    private static readonly AiAdvisorContextDto StudentContext = new()
    {
        DisplayName = "Test Student",
        DepartmentName = "Computer Science",
        AcademicYear = "2025-2026",
        Cgpa = 3.1m,
        CreditsCompleted = 60,
        CreditsRequired = 120
    };

    [Fact]
    public async Task GetAdvisorResponseAsync_ReturnsSuccessfulProviderResponse()
    {
        var client = new FakeOpenAiChatClient
        {
            Response = "You are on track."
        };
        var service = CreateService(client);

        var result = await service.GetAdvisorResponseAsync(
            StudentContext,
            "  Am I on track?  ");

        Assert.True(result.Succeeded);
        Assert.Equal("You are on track.", result.Message);
        Assert.Null(result.ErrorCode);
        Assert.Equal("Am I on track?", client.ReceivedUserMessage);
        Assert.Contains("Test Student", client.ReceivedSystemPrompt);
    }

    [Fact]
    public async Task GetAdvisorResponseAsync_ReturnsConfigurationFailureWithoutCallingProvider()
    {
        var client = new FakeOpenAiChatClient
        {
            IsConfigured = false
        };
        var service = CreateService(client);

        var result = await service.GetAdvisorResponseAsync(StudentContext, "Hello");

        Assert.False(result.Succeeded);
        Assert.Equal("openai_not_configured", result.ErrorCode);
        Assert.Equal(0, client.CallCount);
    }

    [Fact]
    public async Task GetAdvisorResponseAsync_ReturnsFailureForEmptyProviderResponse()
    {
        var client = new FakeOpenAiChatClient
        {
            Response = " "
        };
        var service = CreateService(client);

        var result = await service.GetAdvisorResponseAsync(StudentContext, "Hello");

        Assert.False(result.Succeeded);
        Assert.Equal("openai_empty_response", result.ErrorCode);
    }

    [Fact]
    public async Task GetAdvisorResponseAsync_ReturnsFailureWhenProviderThrows()
    {
        var client = new FakeOpenAiChatClient
        {
            Exception = new InvalidOperationException("Provider failed")
        };
        var service = CreateService(client);

        var result = await service.GetAdvisorResponseAsync(StudentContext, "Hello");

        Assert.False(result.Succeeded);
        Assert.Equal("openai_request_failed", result.ErrorCode);
    }

    [Fact]
    public async Task GetAdvisorResponseAsync_PropagatesCallerCancellation()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        var client = new FakeOpenAiChatClient
        {
            Exception = new OperationCanceledException(cancellationSource.Token)
        };
        var service = CreateService(client);

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            service.GetAdvisorResponseAsync(
                StudentContext,
                "Hello",
                cancellationSource.Token));
    }

    [Fact]
    public async Task GetAdvisorResponseAsync_RejectsBlankMessages()
    {
        var service = CreateService(new FakeOpenAiChatClient());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.GetAdvisorResponseAsync(StudentContext, " "));
    }

    private static AiAdvisorService CreateService(IOpenAiChatClient client) =>
        new(client, NullLogger<AiAdvisorService>.Instance);

    private sealed class FakeOpenAiChatClient : IOpenAiChatClient
    {
        public bool IsConfigured { get; init; } = true;
        public string? Response { get; init; }
        public Exception? Exception { get; init; }
        public int CallCount { get; private set; }
        public string ReceivedSystemPrompt { get; private set; } = string.Empty;
        public string ReceivedUserMessage { get; private set; } = string.Empty;

        public Task<string?> CompleteAsync(
            string systemPrompt,
            string userMessage,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            ReceivedSystemPrompt = systemPrompt;
            ReceivedUserMessage = userMessage;

            if (Exception is not null)
                return Task.FromException<string?>(Exception);

            return Task.FromResult(Response);
        }
    }
}
