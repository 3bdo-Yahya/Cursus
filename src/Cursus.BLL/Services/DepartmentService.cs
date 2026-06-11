using Cursus.Domain.DTOs;
using Cursus.Domain.Entities;
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

        public async Task AddAsync(CreateDepartmentDto request)
        {
            var department = new Department()
            {
                Name = request.Name,
                UniversityId = request.UniversityId,
                TotalCreditsRequired = request.TotalCreditsRequired,
                MinGpaForGraduation = request.MinGpaForGraduation,
                IsActive = request.IsActive
            };
            await _departmentRepository.AddAsync(department);
            await _departmentRepository.SaveChangesAsync();
        }
        public async Task UpdateAsync(EditDepartmentDto request)
        {
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

        public async Task<IEnumerable<DepartmentDto>> GetAllAsync(bool? isActive = null)
        {
            var query = _departmentRepository.GetAll();

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

        public async Task<DepartmentDto?> GetByIdAsync(int id)
        {
            var department = await _departmentRepository.GetById(id)
                .Select(d => new DepartmentDto(
                    d.Id,
                    d.Name,
                    d.UniversityId,
                    d.University!.Name,
                    d.TotalCreditsRequired,
                    d.MinGpaForGraduation,
                    d.IsActive
                )).FirstOrDefaultAsync();

            return department;
        }

        public async Task ToggleActiveAsync(int id)
        {
            var department = await _departmentRepository.GetById(id)
                .FirstOrDefaultAsync();

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
        public Task<bool> ExistsAsync(int id)
            => _departmentRepository.GetAll()
                .AnyAsync(d => d.Id == id);
    }
}