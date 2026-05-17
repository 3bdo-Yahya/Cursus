using Cursus.Domain.DTOs;

namespace Cursus.Domain.Interfaces.Services
{
    public interface IUniversityService
    {
        Task<IEnumerable<UniversityDto>> GetAllAsync();
        Task AddAsync(CreateUniversityDto university);
        Task<bool> IsNameDuplicateAsync(string name, int? excludeId = null);
    }
}