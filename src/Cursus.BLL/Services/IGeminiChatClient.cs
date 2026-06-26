using System.Threading;
using System.Threading.Tasks;

namespace Cursus.BLL.Services;

public interface IGeminiChatClient
{
    bool IsConfigured { get; }

    Task<string?> GenerateContentAsync(string prompt, CancellationToken cancellationToken = default);
}
