using Cursus.Domain.DTOs;

namespace Cursus.Domain.Interfaces.Services
{
    public interface IDepartmentService
    {
        Task<IEnumerable<DepartmentDto>> GetAllAsync(int? universityId = null, bool? isActive = null);
        Task<DepartmentDto?> GetByIdAsync(int id, int? universityId = null);
        Task AddAsync(CreateDepartmentDto request, int? universityId = null);
        Task UpdateAsync(EditDepartmentDto request, int? universityId = null);
        Task ToggleActiveAsync(int id, int? universityId = null);
        Task<bool> IsNameDuplicateAsync(int universityId, string name, int? excludeId = null);
        Task<bool> ExistsAsync(int id, int? universityId = null);
    }
}
