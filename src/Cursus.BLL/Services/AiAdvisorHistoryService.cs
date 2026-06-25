using Cursus.DAL.Database;
using Cursus.Domain.DTOs;
using Cursus.Domain.Entities;
using Cursus.Domain.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace Cursus.BLL.Services;

public sealed class AiAdvisorHistoryService : IAiAdvisorHistoryService
{
    private const int MaxStoredContentLength = 8000;

    private readonly ApplicationDbContext _db;

    public AiAdvisorHistoryService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<AiAdvisorMessageDto>> GetRecentMessagesAsync(
        string studentId,
        int count,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(studentId) || count <= 0)
            return [];

        var messages = await _db.AiAdvisorChatMessages
            .AsNoTracking()
            .Where(message => message.StudentId == studentId)
            .OrderByDescending(message => message.CreatedAtUtc)
            .ThenByDescending(message => message.Id)
            .Take(count)
            .Select(message => new AiAdvisorMessageDto
            {
                Role = message.Role,
                Content = message.Content,
                CreatedAtUtc = message.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return messages
            .OrderBy(message => message.CreatedAtUtc)
            .ToList();
    }

    public async Task SaveExchangeAsync(
        string studentId,
        string userMessage,
        string assistantMessage,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(studentId))
            return;

        var createdAtUtc = DateTimeOffset.UtcNow;

        _db.AiAdvisorChatMessages.AddRange(
            new AiAdvisorChatMessage
            {
                StudentId = studentId,
                Role = "user",
                Content = Truncate(userMessage),
                CreatedAtUtc = createdAtUtc
            },
            new AiAdvisorChatMessage
            {
                StudentId = studentId,
                Role = "assistant",
                Content = Truncate(assistantMessage),
                CreatedAtUtc = createdAtUtc.AddMilliseconds(1)
            });

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task ClearAsync(
        string studentId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(studentId))
            return;

        await _db.AiAdvisorChatMessages
            .Where(message => message.StudentId == studentId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    private static string Truncate(string? value)
    {
        var trimmed = value?.Trim() ?? string.Empty;
        return trimmed.Length <= MaxStoredContentLength
            ? trimmed
            : trimmed[..MaxStoredContentLength];
    }
}
