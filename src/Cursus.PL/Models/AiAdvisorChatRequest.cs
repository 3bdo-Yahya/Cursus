using System.ComponentModel.DataAnnotations;

namespace Cursus.PL.Models;

public sealed class AiAdvisorChatRequest
{
    [Required]
    [StringLength(2000)]
    public string Message { get; init; } = string.Empty;
}
