using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cursus.BLL.Services;
using Cursus.Domain.DTOs;
using Cursus.Domain.Enums;
using Xunit;

namespace Cursus.BLL.Tests
{
    public sealed class GeminiServiceTests
    {
        private static readonly GraduationAuditDto TestAudit = new GraduationAuditDto
        {
            StudentId = "student-123",
            StudentName = "Test Student",
            DepartmentName = "Computer Science",
            AcademicYear = "2025-2026",
            CurrentSemester = SemesterType.Spring,
            CurrentStanding = AcademicStanding.Good,
            EstimatedGradSemester = "Spring 2027",
            MinGpaForGraduation = 2.0m,
            Categories = new List<CategoryProgressDto>()
        };

        private static readonly ChatRequestDto TestRequest = new ChatRequestDto
        {
            Message = "Am I on track?"
        };

        [Fact]
        public async Task AskGeminiAsync_ReturnsSuccessfulResponse()
        {
            // Arrange
            var client = new FakeGeminiChatClient
            {
                Response = "Yes, you are on track."
            };
            var service = new GeminiService(client);

            // Act
            var result = await service.AskGeminiAsync(TestAudit, TestRequest);

            // Assert
            Assert.Equal("Yes, you are on track.", result);
            Assert.Equal(1, client.CallCount);
            Assert.Contains("Test Student", client.ReceivedPrompt);
            Assert.Contains("Am I on track?", client.ReceivedPrompt);
        }

        [Fact]
        public async Task AskGeminiAsync_ReturnsUnconfiguredMessage_WhenClientIsNotConfigured()
        {
            // Arrange
            var client = new FakeGeminiChatClient
            {
                IsConfigured = false
            };
            var service = new GeminiService(client);

            // Act
            var result = await service.AskGeminiAsync(TestAudit, TestRequest);

            // Assert
            Assert.Contains("temporarily unconfigured", result);
            Assert.Equal(0, client.CallCount);
        }

        [Fact]
        public async Task AskGeminiAsync_ReturnsFallbackMessage_WhenClientThrowsException()
        {
            // Arrange
            var client = new FakeGeminiChatClient
            {
                Exception = new InvalidOperationException("API error")
            };
            var service = new GeminiService(client);

            // Act
            var result = await service.AskGeminiAsync(TestAudit, TestRequest);

            // Assert
            Assert.Contains("temporarily unavailable", result);
            Assert.Equal(1, client.CallCount);
        }

        [Fact]
        public async Task AskGeminiAsync_PropagatesCancellation()
        {
            // Arrange
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var client = new FakeGeminiChatClient
            {
                Exception = new OperationCanceledException(cts.Token)
            };
            var service = new GeminiService(client);

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                service.AskGeminiAsync(TestAudit, TestRequest, cts.Token));
        }

        [Fact]
        public async Task AskGeminiAsync_ThrowsArgumentNullException_WhenAuditIsNull()
        {
            var service = new GeminiService(new FakeGeminiChatClient());

            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                service.AskGeminiAsync(null!, TestRequest));
        }

        [Fact]
        public async Task AskGeminiAsync_ThrowsArgumentNullException_WhenRequestIsNull()
        {
            var service = new GeminiService(new FakeGeminiChatClient());

            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                service.AskGeminiAsync(TestAudit, null!));
        }

        private sealed class FakeGeminiChatClient : IGeminiChatClient
        {
            public bool IsConfigured { get; init; } = true;
            public string? Response { get; init; }
            public Exception? Exception { get; init; }
            public int CallCount { get; private set; }
            public string ReceivedPrompt { get; private set; } = string.Empty;

            public Task<string?> GenerateContentAsync(string prompt, CancellationToken cancellationToken = default)
            {
                CallCount++;
                ReceivedPrompt = prompt;

                if (Exception != null)
                {
                    return Task.FromException<string?>(Exception);
                }

                return Task.FromResult(Response);
            }
        }
    }
}
