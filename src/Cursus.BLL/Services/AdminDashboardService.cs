using Cursus.Domain.DTOs;
using Cursus.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Cursus.BLL.Services
{
    public class AdminDashboardService : IAdminDashboardService
    {
        private readonly IGenericRepository<University> _universityRepository;
        private readonly IGenericRepository<GraduationRequirement> _graduationRequirementRepository;
        private readonly IGenericRepository<Department> _departmentRepository;
        private readonly IGenericRepository<Course> _courseRepository;
        private readonly UserManager<AppUser> _userManager;

        public AdminDashboardService(
            IGenericRepository<University> universityRepository,
            IGenericRepository<GraduationRequirement> graduationRequirementRepository,
            IGenericRepository<Department> departmentRepository,
            IGenericRepository<Course> courseRepository,
            UserManager<AppUser> userManager)
        {
            _universityRepository = universityRepository;
            _graduationRequirementRepository = graduationRequirementRepository;
            _departmentRepository = departmentRepository;
            _courseRepository = courseRepository;
            _userManager = userManager;
        }

        public async Task<AdminDashboardDto> GetAdminDashboardAsync()
        {
            var students = await _userManager.GetUsersInRoleAsync("Student");
            return new AdminDashboardDto(
                await _universityRepository.CountAsync(),
                await _graduationRequirementRepository.CountAsync(),
                await _departmentRepository.CountAsync(),
                await _departmentRepository.CountAsync(d => d.IsActive),
                await _departmentRepository.CountAsync(d => !d.IsActive),
                await _courseRepository.CountAsync(),
                await _courseRepository.CountAsync(c => c.IsActive),
                await _courseRepository.CountAsync(c => !c.IsActive),
                students.Count
            );
        }
    }
}