using Cursus.Domain.DTOs;

namespace Cursus.Domain.Interfaces.Services
{
    public interface IAdminDashboardService
    {
        /// <param name="universityId">When set, counts are limited to that university.</param>
        Task<AdminDashboardDto> GetAdminDashboardAsync(int? universityId = null);
    }
}
