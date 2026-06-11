using Cursus.Domain.DTOs;
using Cursus.Domain.Entities;

namespace Cursus.BLL.Services
{
    public class AdminDashboardService : IAdminDashboardService
    {
        private readonly IGenericRepository<University> _universityRepository;
        private readonly IGenericRepository<GraduationRequirement> _graduationRequirementRepository;
        private readonly IGenericRepository<Department> _departmentRepository;
        private readonly IGenericRepository<Course> _courseRepository;
        public AdminDashboardService(
            IGenericRepository<University> universityRepository,
            IGenericRepository<GraduationRequirement> graduationRequirementRepository,
            IGenericRepository<Department> departmentRepository,
            IGenericRepository<Course> courseRepository)
        {
            _universityRepository = universityRepository;
            _graduationRequirementRepository = graduationRequirementRepository;
            _departmentRepository = departmentRepository;
            _courseRepository = courseRepository;
        }
        public async Task<AdminDashboardDto> GetAdminDashboardAsync() => new AdminDashboardDto(
            await _universityRepository.CountAsync(),
await _graduationRequirementRepository.CountAsync(),
await _departmentRepository.CountAsync(),
await _departmentRepository.CountAsync(d => d.IsActive),
await _departmentRepository.CountAsync(d => !d.IsActive),
await _courseRepository.CountAsync(),
await _courseRepository.CountAsync(c => c.IsActive),
        await _courseRepository.CountAsync(c => !c.IsActive)
        );
    }
}