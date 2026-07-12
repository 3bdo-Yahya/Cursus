using Cursus.Domain.DTOs;
using Cursus.Domain.Entities;
using Cursus.Domain.Enums;

namespace Cursus.Domain.Interfaces.Services
{
    public interface IStudentManagementService
    {
        Task<IEnumerable<AppUser>> GetStudentsAsync(string? searchTerm, int? departmentId);

        /// <summary>
        /// Returns all users in the "Student" role, optionally filtered by
        /// department name (partial, case-insensitive).
        /// </summary>
        Task<IEnumerable<AppUser>> GetAllStudentsAsync(string? departmentFilter);

        /// <summary>
        /// Returns the student together with all their <see cref="StudentCourse"/>
        /// records (including the related <see cref="Course"/> and its
        /// <see cref="Department"/>).  Returns <c>null</c> when not found.
        /// </summary>
        Task<AppUser?> GetStudentDetailAsync(string studentId);

        /// <summary>
        /// Creates a student Identity account, assigns the Student role, and
        /// sets academic profile fields (department, university, standing).
        /// </summary>
        Task<StudentCommandResult> CreateStudentAsync(
            CreateStudentRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a user only when they are in the Student role.
        /// </summary>
        Task<StudentCommandResult> DeleteStudentAsync(
            string userId,
            CancellationToken cancellationToken = default);

        /// <summary>Standing breakdown across all students (single pass).</summary>
        Task<StudentStandingSummary> GetStandingSummaryAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a new <see cref="StudentCourse"/> record.
        /// <para>
        /// Grade-to-status resolution rules (when <paramref name="grade"/>
        /// is provided):<br/>
        ///   • grade &gt;= course.PassingGradeThreshold → <c>Completed</c><br/>
        ///   • grade &lt;  course.PassingGradeThreshold → <c>Failed</c><br/>
        /// When <paramref name="grade"/> is <c>null</c> the supplied
        /// <paramref name="status"/> is used as-is (defaults to
        /// <c>InProgress</c> when omitted).
        /// </para>
        /// </summary>
        Task<StudentCourse> AddCourseRecordAsync(
            string studentId,
            int courseId,
            string? grade,
            StudentCourseStatus status,
            SemesterType semester,
            string academicYear);

        /// <summary>
        /// Updates the grade and/or status of an existing
        /// <see cref="StudentCourse"/> record.  When <paramref name="grade"/>
        /// is provided the same grade-to-status resolution as
        /// <see cref="AddCourseRecordAsync"/> is applied automatically.
        /// </summary>
        Task<StudentCourse> UpdateCourseRecordAsync(
            int recordId,
            string? grade,
            StudentCourseStatus status);

        /// <summary>Removes a <see cref="StudentCourse"/> record by its id.</summary>
        Task DeleteCourseRecordAsync(int recordId);
    }
}
