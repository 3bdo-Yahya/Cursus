using Cursus.Domain.DTOs;
using Cursus.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Cursus.BLL.Services
{
    public class UniversityService : IUniversityService
    {
        private readonly IGenericRepository<University> _universityRepository;

        public UniversityService(IGenericRepository<University> universityRepository)
        {
            _universityRepository = universityRepository;
        }
        public async Task<IEnumerable<UniversityDto>> GetAllAsync()
        {
            var universities = await _universityRepository.GetAll()
                .OrderBy(u => u.Name)
                .Select(u => new UniversityDto(
                    u.Id,
                    u.Name,
                    u.Departments.Count
                ))
                .ToListAsync();

            return universities;
        }
        public async Task AddAsync(CreateUniversityDto request)
        {
            var university = new University() { Name = request.Name };
            await _universityRepository.AddAsync(university);
            await _universityRepository.SaveChangesAsync();
        }
        public Task<bool> IsNameDuplicateAsync(string name, int? excludeId = null)
        {
            var normalizedName = name.ToUpper();

            var query = _universityRepository.GetAll()
                .Where(u => u.Name.ToUpper() == normalizedName);

            if (excludeId.HasValue)
                query = query.Where(u => u.Id != excludeId.Value);

            return query.AnyAsync();
        }
    }
}