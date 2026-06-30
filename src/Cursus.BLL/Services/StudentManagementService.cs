using Cursus.DAL.Database;
using Cursus.Domain.Entities;
using Cursus.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Cursus.BLL.Services
{
    public class StudentManagementService : IStudentManagementService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAcademicMetricsService _academicMetricsService;

        // Standard letter-grade ordering — lower index = higher grade.
        // Extend this list if additional grades are used in the institution.
        private static readonly IReadOnlyList<string> GradeOrder = new[]
        {
            "A+", "A", "A-",
            "B+", "B", "B-",
            "C+", "C", "C-",
            "D+", "D", "D-",
            "F"
        };

        public StudentManagementService(ApplicationDbContext context, IAcademicMetricsService academicMetricsService)
        {
            _context = context;
            _academicMetricsService = academicMetricsService;
        }

        // ── GetStudentsAsync (existing, used by the Admin list page) ─────────

        public async Task<IEnumerable<AppUser>> GetStudentsAsync(
            string? searchTerm, int? departmentId)
        {
            var studentRoleId = await _context.Roles
                .Where(r => r.Name == "Student")
                .Select(r => r.Id)
                .FirstOrDefaultAsync();

            if (studentRoleId == null)
                return Enumerable.Empty<AppUser>();

            var query = _context.Users
                .Include(u => u.Department)
                    .ThenInclude(d => d!.University)
                .Where(u => _context.UserRoles
                    .Any(ur => ur.UserId == u.Id && ur.RoleId == studentRoleId))
                .AsNoTracking()
                .AsQueryable();

            if (departmentId.HasValue && departmentId.Value > 0)
                query = query.Where(u => u.DepartmentId == departmentId.Value);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                query = query.Where(u =>
                    (u.UserName != null && u.UserName.ToLower().Contains(term)) ||
                    (u.Email != null && u.Email.ToLower().Contains(term)));
            }

            return await query.OrderBy(u => u.UserName).ToListAsync();
        }

        // ── GetAllStudentsAsync ───────────────────────────────────────────────

        public async Task<IEnumerable<AppUser>> GetAllStudentsAsync(
            string? departmentFilter)
        {
            var studentRoleId = await _context.Roles
                .Where(r => r.Name == "Student")
                .Select(r => r.Id)
                .FirstOrDefaultAsync();

            if (studentRoleId == null)
                return Enumerable.Empty<AppUser>();

            var query = _context.Users
                .Include(u => u.Department)
                    .ThenInclude(d => d!.University)
                .Where(u => _context.UserRoles
                    .Any(ur => ur.UserId == u.Id && ur.RoleId == studentRoleId))
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(departmentFilter))
            {
                var filter = departmentFilter.Trim().ToLower();
                query = query.Where(u =>
                    u.Department != null &&
                    u.Department.Name.ToLower().Contains(filter));
            }

            return await query.OrderBy(u => u.UserName).ToListAsync();
        }

        // ── GetStudentDetailAsync ─────────────────────────────────────────────

        public async Task<AppUser?> GetStudentDetailAsync(string studentId)
        {
            return await _context.Users
                .Include(u => u.Department)
                    .ThenInclude(d => d!.University)
                .Include(u => u.StudentCourses)
                    .ThenInclude(sc => sc.Course)
                        .ThenInclude(c => c!.Department)
                .Include(u => u.StandingHistories)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == studentId);
        }

        // ── AddCourseRecordAsync ──────────────────────────────────────────────

        public async Task<StudentCourse> AddCourseRecordAsync(
            string studentId,
            int courseId,
            string? grade,
            StudentCourseStatus status,
            SemesterType semester,
            string academicYear)
        {
            var (canEnroll, blockReason) = await _academicMetricsService.CanEnrollInCourseAsync(studentId, courseId);
            if (!canEnroll)
            {
                throw new InvalidOperationException(blockReason ?? "Student is not eligible to enroll in this course.");
            }

            var resolvedStatus = ResolveStatus(grade, status, courseId);

            var record = new StudentCourse
            {
                StudentId = studentId,
                CourseId = courseId,
                Grade = NormalizeGrade(grade),
                Status = await resolvedStatus,
                Semester = semester,
                AcademicYear = academicYear
            };

            _context.StudentCourses.Add(record);
            await _context.SaveChangesAsync();
            return record;
        }

        // ── UpdateCourseRecordAsync ───────────────────────────────────────────

        public async Task<StudentCourse> UpdateCourseRecordAsync(
            int recordId,
            string? grade,
            StudentCourseStatus status)
        {
            var record = await _context.StudentCourses
                .Include(sc => sc.Course)
                .FirstOrDefaultAsync(sc => sc.Id == recordId)
                ?? throw new KeyNotFoundException(
                    $"StudentCourse record with id {recordId} was not found.");

            record.Grade = NormalizeGrade(grade);
            record.Status = await ResolveStatus(grade, status, record.CourseId);

            await _context.SaveChangesAsync();
            return record;
        }

        // ── DeleteCourseRecordAsync ───────────────────────────────────────────

        public async Task DeleteCourseRecordAsync(int recordId)
        {
            var record = await _context.StudentCourses.FindAsync(recordId)
                ?? throw new KeyNotFoundException(
                    $"StudentCourse record with id {recordId} was not found.");

            _context.StudentCourses.Remove(record);
            await _context.SaveChangesAsync();
        }

        // ── Private helpers ───────────────────────────────────────────────────

        /// <summary>
        /// Resolves the <see cref="StudentCourseStatus"/> from a grade string.
        /// <list type="bullet">
        ///   <item>No grade supplied → use the caller-provided <paramref name="fallback"/>.</item>
        ///   <item>Grade &gt;= passing threshold  → <c>Completed</c>.</item>
        ///   <item>Grade &lt;  passing threshold  → <c>Failed</c>.</item>
        /// </list>
        /// </summary>
        private async Task<StudentCourseStatus> ResolveStatus(
            string? grade, StudentCourseStatus fallback, int courseId)
        {
            var normalized = NormalizeGrade(grade);

            if (string.IsNullOrEmpty(normalized))
                return fallback;

            // Fetch passing threshold for the course.
            var threshold = await _context.Courses
                .Where(c => c.Id == courseId)
                .Select(c => c.PassingGradeThreshold)
                .FirstOrDefaultAsync()
                ?? "D";

            return IsGradeAtLeast(normalized, threshold)
                ? StudentCourseStatus.Completed
                : StudentCourseStatus.Failed;
        }

        /// <summary>
        /// Returns <c>true</c> when <paramref name="grade"/> is equal to or
        /// better than <paramref name="threshold"/> using the
        /// <see cref="GradeOrder"/> ordering.
        /// </summary>
        private static bool IsGradeAtLeast(string grade, string threshold)
        {
            var gradeIdx = IndexOf(grade);
            var thresholdIdx = IndexOf(threshold);

            // Unknown grades are treated as failing.
            if (gradeIdx < 0 || thresholdIdx < 0)
                return false;

            // Smaller index = better grade.
            return gradeIdx <= thresholdIdx;
        }

        private static int IndexOf(string grade)
        {
            var normalized = grade.Trim().ToUpper();
            for (var i = 0; i < GradeOrder.Count; i++)
            {
                if (string.Equals(GradeOrder[i], normalized, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return -1;
        }

        private static string? NormalizeGrade(string? grade) =>
            string.IsNullOrWhiteSpace(grade) ? null : grade.Trim().ToUpper();
    }
}
