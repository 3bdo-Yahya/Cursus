using Cursus.Domain.DTOs;

namespace Cursus.Domain.Interfaces.Services
{
    public interface IAdminDashboardService
    {
        Task<AdminDashboardDto> GetAdminDashboardAsync();

    }
}