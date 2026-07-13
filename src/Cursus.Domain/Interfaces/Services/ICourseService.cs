using Cursus.Domain.DTOs;

namespace Cursus.Domain.Interfaces.Services
{
    public interface ICourseService
    {
        Task AddAsync(CreateCourseDto request, int? universityId = null);
        Task UpdateAsync(EditCourseDto request, int? universityId = null);
        Task<IEnumerable<CourseDto>> GetAllAsync(int? universityId = null);
        Task<CourseDto?> GetByIdAsync(int id, int? universityId = null);
        Task ToggleActiveAsync(int id, int? universityId = null);
        Task<bool> IsCodeDuplicateAsync(int departmentId, string code, int? excludeId = null);
        Task<bool> ExistsAsync(int id, int? universityId = null);
        Task<bool> DepartmentBelongsToUniversityAsync(int departmentId, int universityId);
    }
}
