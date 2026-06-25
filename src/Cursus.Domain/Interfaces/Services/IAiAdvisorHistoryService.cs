using Cursus.Domain.DTOs;

namespace Cursus.Domain.Interfaces.Services;

public interface IAiAdvisorHistoryService
{
    Task<IReadOnlyList<AiAdvisorMessageDto>> GetRecentMessagesAsync(
        string studentId,
        int count,
        CancellationToken cancellationToken = default);

    Task SaveExchangeAsync(
        string studentId,
        string userMessage,
        string assistantMessage,
        CancellationToken cancellationToken = default);

    Task ClearAsync(
        string studentId,
        CancellationToken cancellationToken = default);
}
