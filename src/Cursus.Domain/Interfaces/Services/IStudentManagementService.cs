using Cursus.Domain.Entities;
using Cursus.Domain.Enums;

namespace Cursus.Domain.Interfaces.Services
{
    public interface IStudentManagementService
    {
        // ── Listing ──────────────────────────────────────────────────────────
        Task<IEnumerable<AppUser>> GetStudentsAsync(string? searchTerm, int? departmentId);

        /// <summary>
        /// Returns all users in the "Student" role, optionally filtered by
        /// department name (partial, case-insensitive).
        /// </summary>
        Task<IEnumerable<AppUser>> GetAllStudentsAsync(string? departmentFilter);

        // ── Detail ───────────────────────────────────────────────────────────
        /// <summary>
        /// Returns the student together with all their <see cref="StudentCourse"/>
        /// records (including the related <see cref="Course"/> and its
        /// <see cref="Department"/>).  Returns <c>null</c> when not found.
        /// </summary>
        Task<AppUser?> GetStudentDetailAsync(string studentId);

        // ── Course-record CRUD ────────────────────────────────────────────────
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
