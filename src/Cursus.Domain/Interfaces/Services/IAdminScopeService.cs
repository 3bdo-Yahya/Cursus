using Cursus.Domain;

namespace Cursus.Domain.Interfaces.Services;

public interface IAdminScopeService
{
    /// <summary>
    /// Resolves admin/super-admin flags and the linked university for the given user id.
    /// </summary>
    Task<AdminScope> ResolveAsync(string userId, CancellationToken cancellationToken = default);
}
