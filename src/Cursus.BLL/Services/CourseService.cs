using Cursus.Domain.DTOs;
using Cursus.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Cursus.BLL.Services.Implementation
{
    public class CourseService : ICourseService
    {
        private readonly IGenericRepository<Course> _courseRepository;

        public CourseService(IGenericRepository<Course> courseRepository)
        {
            _courseRepository = courseRepository;
        }

        public async Task AddAsync(CreateCourseDto request)
        {
            var course = new Course()
            {
                Code = request.Code,
                Name = request.Name,
                DepartmentId = request.DepartmentId,
                CreditHours = request.CreditHours,
                PassingGradeThreshold = request.PassingGradeThreshold,
                CourseType = request.CourseType,
                SemesterAvailability = request.SemesterAvailability,
                IsActive = request.IsActive
            };
            await _courseRepository.AddAsync(course);
            await _courseRepository.SaveChangesAsync();
        }

        public async Task<IEnumerable<CourseDto>> GetAllAsync()
        {
            var courses = await _courseRepository.GetAll()
            .Select(c => new CourseDto(
                c.Id,
                c.Code,
                c.Name,
                c.CreditHours,
                c.CourseType,
                c.SemesterAvailability,
                c.PassingGradeThreshold,
                c.DepartmentId,
                c.IsActive,
        c.Prerequisites.Select(p => new CoursePrerequisiteDto(
                        p.PrerequisiteId,
                        p.Prerequisite!.Code,
                        p.Prerequisite!.Name
                    )))).ToListAsync();

            return courses;
        }

        public async Task<CourseDto?> GetByIdAsync(int id)
        {
            return await _courseRepository.GetById(id)
                .Select(c => new CourseDto(
                    c.Id,
                    c.Code,
                    c.Name,
                    c.CreditHours,
                    c.CourseType,
                    c.SemesterAvailability,
                    c.PassingGradeThreshold,
                    c.DepartmentId,
                    c.IsActive,
                    c.Prerequisites.Select(p => new CoursePrerequisiteDto(
                        p.PrerequisiteId,
                        p.Prerequisite!.Code,
                        p.Prerequisite!.Name
                    ))
                )).FirstOrDefaultAsync();
        }

        public async Task ToggleActiveAsync(int id)
        {
            var course = await _courseRepository.GetById(id)
                .FirstOrDefaultAsync();

            if (course is null)
                return;

            course.IsActive = !course.IsActive;
            _courseRepository.Update(course);
            await _courseRepository.SaveChangesAsync();
        }

        public async Task UpdateAsync(EditCourseDto request)
        {
            var course = new Course()
            {
                Id = request.Id,
                Code = request.Code,
                Name = request.Name,
                DepartmentId = request.DepartmentId,
                CreditHours = request.CreditHours,
                PassingGradeThreshold = request.PassingGradeThreshold,
                CourseType = request.CourseType,
                SemesterAvailability = request.SemesterAvailability,
                IsActive = request.IsActive
            };
            _courseRepository.Update(course);
            await _courseRepository.SaveChangesAsync();
        }
        public Task<bool> IsCodeDuplicateAsync(int departmentId, string code, int? excludeId = null)
        {
            var normalizedCode = code.ToUpper();

            var query = _courseRepository.GetAll()
                .Where(c => c.DepartmentId == departmentId &&
                            c.Code.ToUpper() == normalizedCode);

            if (excludeId.HasValue)
                query = query.Where(c => c.Id != excludeId.Value);

            return query.AnyAsync();
        }
        public Task<bool> ExistsAsync(int id)
            => _courseRepository.GetAll()
                .AnyAsync(c => c.Id == id);
    }
}