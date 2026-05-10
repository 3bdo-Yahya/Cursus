using Cursus.Domain.Enums;

namespace Cursus.BLL;

public interface IStudentManagementService
{
    Task<IReadOnlyList<StudentListItemDto>> GetAllStudentsAsync(int? departmentId);

    Task<StudentDetailDto?> GetStudentDetailAsync(string studentId);

    Task<StudentCourseMutationResult> AddCourseRecordAsync(string studentId, int courseId, string? grade, StudentCourseStatus status, SemesterType semester, string academicYear);

    Task<StudentCourseMutationResult> UpdateCourseRecordAsync(int recordId, string? grade, StudentCourseStatus status);

    Task<StudentCourseMutationResult> DeleteCourseRecordAsync(int recordId);
}
