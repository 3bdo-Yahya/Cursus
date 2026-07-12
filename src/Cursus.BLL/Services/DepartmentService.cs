using Cursus.BLL.Services;
using Cursus.Domain.DTOs;
using Cursus.Domain.Entities;
using Cursus.Domain.Interfaces.Repositories;
using Cursus.Domain.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace Cursus.BLL.Services.Implementation
{
    public class DepartmentService : IDepartmentService
    {
        private readonly IGenericRepository<Department> _departmentRepository;

        public DepartmentService(IGenericRepository<Department> departmentRepository)
        {
            _departmentRepository = departmentRepository;
        }

        public async Task AddAsync(CreateDepartmentDto request, int? universityId = null)
        {
            var resolvedUniversityId = universityId ?? request.UniversityId;
            if (universityId.HasValue && request.UniversityId != universityId.Value && request.UniversityId > 0)
            {
                throw new InvalidOperationException(
                    "Cannot create a department for another university.");
            }

            var department = new Department()
            {
                Name = request.Name,
                UniversityId = resolvedUniversityId,
                TotalCreditsRequired = request.TotalCreditsRequired,
                MinGpaForGraduation = request.MinGpaForGraduation,
                IsActive = request.IsActive
            };
            await _departmentRepository.AddAsync(department);
            await _departmentRepository.SaveChangesAsync();
        }

        public async Task UpdateAsync(EditDepartmentDto request, int? universityId = null)
        {
            if (universityId.HasValue)
            {
                var existing = await GetByIdAsync(request.Id, universityId);
                if (existing is null)
                    throw new KeyNotFoundException($"Department {request.Id} was not found in scope.");

                // University admins cannot move departments across universities.
                request.UniversityId = universityId.Value;
            }

            var department = new Department()
            {
                Id = request.Id,
                Name = request.Name,
                UniversityId = request.UniversityId,
                TotalCreditsRequired = request.TotalCreditsRequired,
                MinGpaForGraduation = request.MinGpaForGraduation,
                IsActive = request.IsActive
            };
            _departmentRepository.Update(department);
            await _departmentRepository.SaveChangesAsync();
        }

        public async Task<IEnumerable<DepartmentDto>> GetAllAsync(
            int? universityId = null, bool? isActive = null)
        {
            var query = _departmentRepository.GetAll();

            if (universityId.HasValue)
                query = UniversityScope.ForUniversity(query, universityId.Value);

            if (isActive.HasValue)
                query = query.Where(d => d.IsActive == isActive.Value);

            return await query
                .OrderBy(d => d.Name)
                .Select(d => new DepartmentDto(
                    d.Id,
                    d.Name,
                    d.UniversityId,
                    d.University!.Name,
                    d.TotalCreditsRequired,
                    d.MinGpaForGraduation,
                    d.IsActive
                )).ToListAsync();
        }

        public async Task<DepartmentDto?> GetByIdAsync(int id, int? universityId = null)
        {
            var query = _departmentRepository.GetById(id);
            if (universityId.HasValue)
                query = UniversityScope.ForUniversity(query, universityId.Value);

            return await query
                .Select(d => new DepartmentDto(
                    d.Id,
                    d.Name,
                    d.UniversityId,
                    d.University!.Name,
                    d.TotalCreditsRequired,
                    d.MinGpaForGraduation,
                    d.IsActive
                )).FirstOrDefaultAsync();
        }

        public async Task ToggleActiveAsync(int id, int? universityId = null)
        {
            var query = _departmentRepository.GetById(id);
            if (universityId.HasValue)
                query = UniversityScope.ForUniversity(query, universityId.Value);

            var department = await query.FirstOrDefaultAsync();
            if (department is null)
                return;

            department.IsActive = !department.IsActive;
            _departmentRepository.Update(department);
            await _departmentRepository.SaveChangesAsync();
        }

        public Task<bool> IsNameDuplicateAsync(int universityId, string name, int? excludeId = null)
        {
            var normalizedName = name.ToUpper();

            var query = _departmentRepository.GetAll()
                .Where(d => d.UniversityId == universityId &&
                            d.Name.ToUpper() == normalizedName);

            if (excludeId.HasValue)
                query = query.Where(d => d.Id != excludeId.Value);

            return query.AnyAsync();
        }

        public Task<bool> ExistsAsync(int id, int? universityId = null)
        {
            var query = _departmentRepository.GetAll().Where(d => d.Id == id);
            if (universityId.HasValue)
                query = UniversityScope.ForUniversity(query, universityId.Value);
            return query.AnyAsync();
        }
    }
}
