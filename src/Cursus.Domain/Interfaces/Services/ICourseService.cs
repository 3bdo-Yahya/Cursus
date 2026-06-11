using Cursus.Domain.DTOs;

namespace Cursus.Domain.Interfaces.Services
{
    public interface ICourseService
    {
        Task AddAsync(CreateCourseDto request);
        Task UpdateAsync(EditCourseDto request);
        Task<IEnumerable<CourseDto>> GetAllAsync();
        Task<CourseDto?> GetByIdAsync(int id);
        Task ToggleActiveAsync(int id);
        Task<bool> IsCodeDuplicateAsync(int departmentId, string code, int? excludeId = null);
        Task<bool> ExistsAsync(int id);
    }
}