using Cursus.Domain.DTOs;

namespace Cursus.Domain.Interfaces.Services
{
    public interface IStudentDashboardService
    {
        Task<StudentDashboardDto?> GetDashboardDataAsync(string studentId);
    }
}
