using Cursus.Domain.DTOs;
using Cursus.Domain.Entities;
using Cursus.Domain.Enums;

namespace Cursus.Domain.Interfaces.Services
{
    public interface IStudentManagementService
    {
        Task<IEnumerable<AppUser>> GetStudentsAsync(
            string? searchTerm, int? departmentId, int? universityId = null);

        /// <summary>
        /// Returns all users in the "Student" role, optionally filtered by
        /// department name (partial, case-insensitive) and university.
        /// </summary>
        Task<IEnumerable<AppUser>> GetAllStudentsAsync(
            string? departmentFilter, int? universityId = null);

        /// <summary>
        /// Returns the student together with all their <see cref="StudentCourse"/>
        /// records. Returns <c>null</c> when not found or outside <paramref name="universityId"/>.
        /// </summary>
        Task<AppUser?> GetStudentDetailAsync(string studentId, int? universityId = null);

        Task<StudentCommandResult> CreateStudentAsync(
            CreateStudentRequest request,
            int? universityId = null,
            CancellationToken cancellationToken = default);

        Task<StudentCommandResult> DeleteStudentAsync(
            string userId,
            int? universityId = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Places a not-yet-assigned student (onboarding-only; rejects when already placed).
        /// </summary>
        Task<StudentCommandResult> CompleteOnboardingAsync(
            CompleteOnboardingRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>Standing breakdown across students in scope (single pass).</summary>
        Task<StudentStandingSummary> GetStandingSummaryAsync(
            int? universityId = null,
            CancellationToken cancellationToken = default);

        Task<StudentCourse> AddCourseRecordAsync(
            string studentId,
            int courseId,
            string? grade,
            StudentCourseStatus status,
            SemesterType semester,
            string academicYear);

        Task<StudentCourse> UpdateCourseRecordAsync(
            int recordId,
            string? grade,
            StudentCourseStatus status);

        Task DeleteCourseRecordAsync(int recordId);
    }
}

