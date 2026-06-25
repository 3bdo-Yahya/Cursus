using System.ComponentModel.DataAnnotations;
using Cursus.Domain.DTOs;

namespace Cursus.PL.Models;

public sealed class AiAdvisorChatRequest
{
    [Required]
    [StringLength(2000)]
    public string Message { get; init; } = string.Empty;

    public IReadOnlyList<AiAdvisorMessageDto> History { get; init; } = [];
}
