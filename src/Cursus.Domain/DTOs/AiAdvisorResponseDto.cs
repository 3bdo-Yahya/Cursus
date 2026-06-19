namespace Cursus.Domain.DTOs;

/// <summary>
/// Result returned by the AI advisor service.
/// </summary>
public sealed class AiAdvisorResponseDto
{
    public bool Succeeded { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? ErrorCode { get; init; }

    public static AiAdvisorResponseDto Success(string message) =>
        new()
        {
            Succeeded = true,
            Message = message
        };

    public static AiAdvisorResponseDto Failure(string errorCode, string message) =>
        new()
        {
            Succeeded = false,
            ErrorCode = errorCode,
            Message = message
        };
}
