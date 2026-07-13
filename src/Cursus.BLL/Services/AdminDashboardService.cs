using Cursus.Domain.DTOs;
using Cursus.Domain.Entities;
using Cursus.Domain.Interfaces.Repositories;
using Cursus.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

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

        public async Task<AdminDashboardDto> GetAdminDashboardAsync(int? universityId = null)
        {
            var students = await _userManager.GetUsersInRoleAsync("Student");
            if (universityId.HasValue)
                students = students.Where(s => s.UniversityId == universityId.Value).ToList();

            var departments = _departmentRepository.GetAll();
            var courses = _courseRepository.GetAll();
            var graduationRequirements = _graduationRequirementRepository.GetAll();

            if (universityId.HasValue)
            {
                departments = UniversityScope.ForUniversity(departments, universityId.Value);
                courses = UniversityScope.ForUniversity(courses, universityId.Value);
                graduationRequirements = UniversityScope.ForUniversity(
                    graduationRequirements, universityId.Value);
            }

            var totalUniversities = universityId.HasValue
                ? 1
                : await _universityRepository.CountAsync();

            return new AdminDashboardDto(
                totalUniversities,
                await graduationRequirements.CountAsync(),
                await departments.CountAsync(),
                await departments.CountAsync(d => d.IsActive),
                await departments.CountAsync(d => !d.IsActive),
                await courses.CountAsync(),
                await courses.CountAsync(c => c.IsActive),
                await courses.CountAsync(c => !c.IsActive),
                students.Count
            );
        }
    }
}
