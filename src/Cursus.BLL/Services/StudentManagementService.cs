using Cursus.DAL.Database;
using Cursus.Domain.Constants;
using Cursus.Domain.DTOs;
using Cursus.Domain.Entities;
using Cursus.Domain.Enums;
using Cursus.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Cursus.BLL.Services
{
    public class StudentManagementService : IStudentManagementService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAcademicMetricsService _academicMetricsService;
        private readonly UserManager<AppUser> _userManager;

        // Standard letter-grade ordering — lower index = higher grade.
        private static readonly IReadOnlyList<string> GradeOrder = new[]
        {
            "A+", "A", "A-",
            "B+", "B", "B-",
            "C+", "C", "C-",
            "D+", "D", "D-",
            "F"
        };

        public StudentManagementService(
            ApplicationDbContext context,
            IAcademicMetricsService academicMetricsService,
            UserManager<AppUser> userManager)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _academicMetricsService = academicMetricsService ?? throw new ArgumentNullException(nameof(academicMetricsService));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        }

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

        public async Task<StudentCommandResult> CreateStudentAsync(
            CreateStudentRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var email = request.Email.Trim();
            var normalizedEmail = email.ToLowerInvariant();

            var existing = await _userManager.FindByEmailAsync(normalizedEmail);
            if (existing is not null)
            {
                return StudentCommandResult.Failure(
                    "A student with this email address already exists.",
                    nameof(CreateStudentRequest.Email));
            }

            var department = await _context.Departments
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    d => d.Id == request.DepartmentId && d.IsActive,
                    cancellationToken);

            if (department is null)
            {
                return StudentCommandResult.Failure(
                    "Please select a valid active department.",
                    nameof(CreateStudentRequest.DepartmentId));
            }

            var user = new AppUser
            {
                UserName = normalizedEmail,
                Email = email,
                EmailConfirmed = true,
                UniversityId = department.UniversityId,
                DepartmentId = department.Id,
                AcademicYear = request.AcademicYear.Trim(),
                CurrentSemester = request.CurrentSemester,
                CurrentStanding = AcademicStanding.Good,
                EnrollmentDate = request.EnrollmentDate
            };

            var createResult = await _userManager.CreateAsync(user, request.Password);
            if (!createResult.Succeeded)
            {
                return StudentCommandResult.Failures(
                    createResult.Errors.Select(e => e.Description));
            }

            try
            {
                var roleResult = await _userManager.AddToRoleAsync(user, Roles.Student);
                if (!roleResult.Succeeded)
                {
                    await _userManager.DeleteAsync(user);
                    return StudentCommandResult.Failures(
                        roleResult.Errors.Select(e => e.Description));
                }
            }
            catch (InvalidOperationException)
            {
                await _userManager.DeleteAsync(user);
                return StudentCommandResult.Failure(
                    $"The role \u201c{Roles.Student}\u201d is not configured. Contact an administrator.");
            }

            return StudentCommandResult.Success(user.DisplayName);
        }

        public async Task<StudentCommandResult> DeleteStudentAsync(
            string userId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return StudentCommandResult.Failure("Student id is required.");

            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
                return StudentCommandResult.Failure("Student not found.");

            if (!await _userManager.IsInRoleAsync(user, Roles.Student))
            {
                return StudentCommandResult.Failure(
                    "Only accounts in the Student role can be deleted from student management.");
            }

            var displayName = user.DisplayName;
            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                return StudentCommandResult.Failures(
                    result.Errors.Select(e => e.Description));
            }

            return StudentCommandResult.Success(displayName);
        }

        public async Task<StudentStandingSummary> GetStandingSummaryAsync(
            CancellationToken cancellationToken = default)
        {
            var students = (await GetStudentsAsync(null, null)).ToList();
            return new StudentStandingSummary(
                Total: students.Count,
                Good: students.Count(s => s.CurrentStanding == AcademicStanding.Good),
                WarningOrProbation: students.Count(s =>
                    s.CurrentStanding is AcademicStanding.Warning or AcademicStanding.Probation),
                Dismissed: students.Count(s => s.CurrentStanding == AcademicStanding.Dismissed));
        }

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

            var resolvedStatus = await ResolveStatus(grade, status, record.CourseId);
            var normalizedGrade = NormalizeGrade(grade);

            var needsEnrollmentCheck = resolvedStatus == StudentCourseStatus.InProgress
                || (resolvedStatus == StudentCourseStatus.Completed
                    && !string.IsNullOrEmpty(normalizedGrade)
                    && !IsRetakeEligibleGrade(normalizedGrade));

            if (needsEnrollmentCheck)
            {
                var (canEnroll, blockReason) = await _academicMetricsService.CanEnrollInCourseAsync(
                    record.StudentId,
                    record.CourseId,
                    excludeStudentCourseId: recordId);

                if (!canEnroll)
                {
                    throw new InvalidOperationException(
                        blockReason ?? "Student is not eligible to enroll in this course.");
                }
            }

            record.Grade = normalizedGrade;
            record.Status = resolvedStatus;

            await _context.SaveChangesAsync();
            return record;
        }

        public async Task DeleteCourseRecordAsync(int recordId)
        {
            var record = await _context.StudentCourses.FindAsync(recordId)
                ?? throw new KeyNotFoundException(
                    $"StudentCourse record with id {recordId} was not found.");

            _context.StudentCourses.Remove(record);
            await _context.SaveChangesAsync();
        }

        private async Task<StudentCourseStatus> ResolveStatus(
            string? grade, StudentCourseStatus fallback, int courseId)
        {
            var normalized = NormalizeGrade(grade);

            if (string.IsNullOrEmpty(normalized))
                return fallback;

            var threshold = await _context.Courses
                .Where(c => c.Id == courseId)
                .Select(c => c.PassingGradeThreshold)
                .FirstOrDefaultAsync()
                ?? "D";

            return IsGradeAtLeast(normalized, threshold)
                ? StudentCourseStatus.Completed
                : StudentCourseStatus.Failed;
        }

        private static bool IsGradeAtLeast(string grade, string threshold)
        {
            var gradeIdx = IndexOf(grade);
            var thresholdIdx = IndexOf(threshold);

            if (gradeIdx < 0 || thresholdIdx < 0)
                return false;

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

        private static bool IsRetakeEligibleGrade(string grade) =>
            grade is "D+" or "D" or "D-" or "F";
    }
}
