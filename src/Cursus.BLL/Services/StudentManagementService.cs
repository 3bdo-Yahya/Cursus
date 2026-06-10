using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Cursus.Domain.Entities;
using Cursus.DAL.Database;
using Cursus.BLL.Interfaces;

namespace Cursus.BLL.Services
{
    public class StudentManagementService : IStudentManagementService
    {
        private readonly ApplicationDbContext _context;

        public StudentManagementService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<AppUser>> GetStudentsAsync(string? searchTerm, int? departmentId)
        {
            var studentRoleId = await _context.Roles
                .Where(r => r.Name == "Student")
                .Select(r => r.Id)
                .FirstOrDefaultAsync();

            if (studentRoleId == null)
            {
                return Enumerable.Empty<AppUser>();
            }

            var query = _context.Users
                .Include(u => u.Department)
                    .ThenInclude(d => d!.University)
                .Where(user => _context.UserRoles.Any(ur => ur.UserId == user.Id && ur.RoleId == studentRoleId))
                .AsNoTracking()
                .AsQueryable();

            if (departmentId.HasValue && departmentId.Value > 0)
            {
                query = query.Where(u => u.DepartmentId == departmentId.Value);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                query = query.Where(u =>
                    (u.UserName != null && u.UserName.ToLower().Contains(term)) ||
                    (u.Email != null && u.Email.ToLower().Contains(term))
                );
            }

            return await query
                .OrderBy(u => u.UserName)
                .ToListAsync();
        }
    }
}
