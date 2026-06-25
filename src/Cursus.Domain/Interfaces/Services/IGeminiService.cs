using Cursus.Domain.DTOs;
using System.Threading;
using System.Threading.Tasks;

namespace Cursus.Domain.Interfaces.Services
{
    public interface IGeminiService
    {
        Task<string> AskGeminiAsync(GraduationAuditDto audit, ChatRequestDto request, CancellationToken cancellationToken = default);
    }
}