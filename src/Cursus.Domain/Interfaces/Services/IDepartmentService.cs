using Cursus.Domain.DTOs;

namespace Cursus.Domain.Interfaces.Services
{
    public interface IDepartmentService
    {
        Task<IEnumerable<DepartmentDto>> GetAllAsync(bool? isActive = null);
        Task<DepartmentDto?> GetByIdAsync(int id);
        Task AddAsync(CreateDepartmentDto request);
        Task UpdateAsync(EditDepartmentDto request);
        Task ToggleActiveAsync(int id);
        Task<bool> IsNameDuplicateAsync(int universityId, string name, int? excludeId = null);
        Task<bool> ExistsAsync(int id);
    }
}