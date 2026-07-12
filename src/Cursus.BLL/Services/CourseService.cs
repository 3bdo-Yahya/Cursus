using Cursus.BLL.Services;
using Cursus.Domain.DTOs;
using Cursus.Domain.Entities;
using Cursus.Domain.Interfaces.Repositories;
using Cursus.Domain.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace Cursus.BLL.Services.Implementation
{
    public class CourseService : ICourseService
    {
        private readonly IGenericRepository<Course> _courseRepository;
        private readonly IGenericRepository<Department> _departmentRepository;

        public CourseService(
            IGenericRepository<Course> courseRepository,
            IGenericRepository<Department> departmentRepository)
        {
            _courseRepository = courseRepository;
            _departmentRepository = departmentRepository;
        }

        public async Task AddAsync(CreateCourseDto request, int? universityId = null)
        {
            if (universityId.HasValue &&
                !await DepartmentBelongsToUniversityAsync(request.DepartmentId, universityId.Value))
            {
                throw new InvalidOperationException(
                    "Department does not belong to the administrator's university.");
            }

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

        public async Task<IEnumerable<CourseDto>> GetAllAsync(int? universityId = null)
        {
            var query = _courseRepository.GetAll();
            if (universityId.HasValue)
                query = UniversityScope.ForUniversity(query, universityId.Value);

            return await query
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
        }

        public async Task<CourseDto?> GetByIdAsync(int id, int? universityId = null)
        {
            var query = _courseRepository.GetById(id);
            if (universityId.HasValue)
                query = UniversityScope.ForUniversity(query, universityId.Value);

            return await query
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

        public async Task ToggleActiveAsync(int id, int? universityId = null)
        {
            var query = _courseRepository.GetById(id);
            if (universityId.HasValue)
                query = UniversityScope.ForUniversity(query, universityId.Value);

            var course = await query.FirstOrDefaultAsync();
            if (course is null)
                return;

            course.IsActive = !course.IsActive;
            _courseRepository.Update(course);
            await _courseRepository.SaveChangesAsync();
        }

        public async Task UpdateAsync(EditCourseDto request, int? universityId = null)
        {
            if (universityId.HasValue)
            {
                var existing = await GetByIdAsync(request.Id, universityId);
                if (existing is null)
                    throw new KeyNotFoundException($"Course {request.Id} was not found in scope.");

                if (!await DepartmentBelongsToUniversityAsync(request.DepartmentId, universityId.Value))
                {
                    throw new InvalidOperationException(
                        "Department does not belong to the administrator's university.");
                }
            }

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

        public Task<bool> ExistsAsync(int id, int? universityId = null)
        {
            var query = _courseRepository.GetAll().Where(c => c.Id == id);
            if (universityId.HasValue)
                query = UniversityScope.ForUniversity(query, universityId.Value);
            return query.AnyAsync();
        }

        public Task<bool> DepartmentBelongsToUniversityAsync(int departmentId, int universityId) =>
            _departmentRepository.GetAll()
                .AnyAsync(d => d.Id == departmentId && d.UniversityId == universityId);
    }
}
