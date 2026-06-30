using Cursus.Domain.DTOs;

namespace Cursus.BLL.Interfaces;

public interface IStudentPortalService
{
    Task<StudentPortalSnapshot?> GetSnapshotAsync(string studentId);
}
