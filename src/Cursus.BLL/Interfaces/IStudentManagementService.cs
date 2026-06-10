using System.Collections.Generic;
using System.Threading.Tasks;
using Cursus.Domain.Entities;

namespace Cursus.BLL.Interfaces
{
    public interface IStudentManagementService
    {
        Task<IEnumerable<AppUser>> GetStudentsAsync(string? searchTerm, int? departmentId);
    }
}
