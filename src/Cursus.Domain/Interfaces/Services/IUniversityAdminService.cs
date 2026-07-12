using Cursus.Domain.DTOs;

namespace Cursus.Domain.Interfaces.Services;

public interface IUniversityAdminService
{
    Task<IReadOnlyList<UniversityAdminDto>> GetAdminsAsync(int? universityId = null);

    Task<int> CountAsync();

    Task<UniversityAdminCommandResult> CreateAdminAsync(
        CreateUniversityAdminRequest request,
        CancellationToken cancellationToken = default);

    Task<UniversityAdminCommandResult> UpdateUniversityAsync(
        string userId,
        int universityId,
        CancellationToken cancellationToken = default);

    Task<UniversityAdminCommandResult> DeleteAdminAsync(
        string userId,
        CancellationToken cancellationToken = default);
}
