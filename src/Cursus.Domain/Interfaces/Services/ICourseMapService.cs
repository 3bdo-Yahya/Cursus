using Cursus.Domain.DTOs;

namespace Cursus.Domain.Interfaces.Services
{
    public interface ICourseMapService
    {
        Task<CourseGraphDto> GetCourseGraphForStudentAsync(string? studentId, int departmentId);
    }
}