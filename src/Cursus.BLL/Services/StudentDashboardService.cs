using Cursus.DAL.Database;
using Cursus.Domain.DTOs;
using Cursus.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Cursus.BLL.Services
{
    public class StudentDashboardService : IStudentDashboardService
    {
        private const int DefaultMaxCreditsPerSemester = 18;

        private readonly ApplicationDbContext _context;

        public StudentDashboardService(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<StudentDashboardDto?> GetDashboardDataAsync(string studentId)
        {
            var student = await _context.Users
                .Include(u => u.Department)
                .Include(u => u.StudentCourses)
                    .ThenInclude(sc => sc.Course)
                .Include(u => u.StandingHistories)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == studentId);

            if (student is null)
                return null;

            var gradeScale = student.Department?.UniversityId is int uniId
                ? await _context.GradeScales
                    .Where(gs => gs.UniversityId == uniId)
                    .AsNoTracking()
                    .ToDictionaryAsync(gs => gs.LetterGrade.ToUpper(), gs => gs.PointValue)
                : BuildDefaultGradeScale();

            var allRecords     = student.StudentCourses.ToList();
            var completedRecs  = allRecords.Where(r => r.Status == StudentCourseStatus.Completed).ToList();
            var gradedRecs     = allRecords.Where(r => r.Status is StudentCourseStatus.Completed
                                                                  or StudentCourseStatus.Failed
                                                     && r.Grade  != null).ToList();
            var inProgressRecs = allRecords.Where(r => r.Status == StudentCourseStatus.InProgress).ToList();

            var cgpa = CalculateCgpa(gradedRecs, gradeScale);

            var cgpaChange = CalculateCgpaChange(student.StandingHistories.ToList(), cgpa);

            var creditsCompleted = completedRecs
                .Where(r => r.Course != null)
                .Sum(r => r.Course!.CreditHours);

            var creditsRequired = student.Department?.TotalCreditsRequired ?? 0;

            var completedCourseIds = completedRecs
                .Where(r => r.CourseId > 0)
                .Select(r => r.CourseId)
                .ToHashSet();

            int coreRemaining     = 0;
            int electiveRemaining = 0;
            int uniReqRemaining   = 0;

            if (student.Department != null)
            {
                var deptCourses = await _context.Courses
                    .Where(c => c.DepartmentId == student.Department.Id && c.IsActive)
                    .AsNoTracking()
                    .ToListAsync();

                coreRemaining = deptCourses
                    .Count(c => c.CourseType == CourseType.Core
                             && !completedCourseIds.Contains(c.Id));

                electiveRemaining = deptCourses
                    .Count(c => c.CourseType is CourseType.DeptElective or CourseType.FreeElective
                             && !completedCourseIds.Contains(c.Id));

                uniReqRemaining = deptCourses
                    .Count(c => c.CourseType == CourseType.UniversityReq
                             && !completedCourseIds.Contains(c.Id));
            }

            int coursesRemaining = coreRemaining + electiveRemaining + uniReqRemaining;

            var standing = student.CurrentStanding;

            var (projectedGraduation, totalSemesters) = ProjectGraduation(
                creditsCompleted, creditsRequired,
                student.CurrentSemester, student.AcademicYear,
                cgpa, standing);

            var semestersCompleted = allRecords
                .Select(r => (r.Semester, r.AcademicYear))
                .Distinct()
                .Count();

            var currentCourses = inProgressRecs
                .Where(r => r.Course != null)
                .Select(r => new EnrolledCourseDto(
                    r.Course!.Code,
                    r.Course!.Name,
                    r.Course!.CreditHours,
                    r.Course!.CourseType != CourseType.Core
                ))
                .ToList();

            return new StudentDashboardDto
            {
                StudentId                = student.Id,
                DisplayName              = student.DisplayName,
                DepartmentName           = student.Department?.Name ?? "Not assigned",
                AcademicYear             = student.AcademicYear    ?? string.Empty,
                CurrentSemester          = student.CurrentSemester,
                Standing                 = standing,

                Cgpa                     = cgpa,
                CgpaChange               = cgpaChange,

                CreditsCompleted         = creditsCompleted,
                CreditsRequired          = creditsRequired,

                CoursesRemaining         = coursesRemaining,
                CoreCoursesRemaining     = coreRemaining,
                ElectiveCoursesRemaining = electiveRemaining,
                UniReqCoursesRemaining   = uniReqRemaining,

                ProjectedGraduation      = projectedGraduation,
                SemestersCompleted       = semestersCompleted,
                TotalSemesters           = totalSemesters,

                CurrentCourses           = currentCourses
            };
        }
        private static decimal CalculateCgpa(
            IEnumerable<Domain.Entities.StudentCourse> gradedRecords,
            Dictionary<string, decimal> gradeScale)
        {
            decimal totalPoints  = 0;
            int     totalCredits = 0;

            foreach (var rec in gradedRecords)
            {
                if (rec.Course is null || rec.Grade is null)
                    continue;

                var key = rec.Grade.Trim().ToUpper();
                if (!gradeScale.TryGetValue(key, out var points))
                    continue;

                totalPoints  += rec.Course.CreditHours * points;
                totalCredits += rec.Course.CreditHours;
            }

            return totalCredits == 0 ? 0m : Math.Round(totalPoints / totalCredits, 2);
        }

        private static decimal CalculateCgpaChange(
            IList<Domain.Entities.StandingHistory> history,
            decimal currentCgpa)
        {
            if (history.Count == 0)
                return 0m;

            var lastEntry = history
                .OrderByDescending(h => h.AcademicYear)
                .ThenByDescending(h => h.Semester)
                .First();

            return Math.Round(currentCgpa - lastEntry.CumulativeGpa, 2);
        }

        private static (string Label, int TotalSemesters) ProjectGraduation(
            int             creditsCompleted,
            int             creditsRequired,
            SemesterType    currentSemester,
            string?         academicYear,
            decimal         cgpa,
            AcademicStanding standing)
        {
            if (creditsRequired <= 0)
                return ("N/A", 0);

            var creditsRemaining = Math.Max(0, creditsRequired - creditsCompleted);

            if (creditsRemaining == 0)
                return ("Completed", 0);

            var maxPerSemester = standing switch
            {
                AcademicStanding.Probation => 12,
                AcademicStanding.Warning   => 18,
                _ when cgpa >= 3.0m        => 21,   // Good standing + overload eligible
                _                          => DefaultMaxCreditsPerSemester
            };

            var semestersNeeded = (int)Math.Ceiling((double)creditsRemaining / maxPerSemester);

            if (!int.TryParse(academicYear?.Split('-').FirstOrDefault(), out var startYear))
                startYear = DateTime.UtcNow.Year;

            var semester = currentSemester;
            var year     = startYear;

            for (var i = 0; i < semestersNeeded; i++)
                (semester, year) = AdvanceSemester(semester, year);

            var semLabel = semester switch
            {
                SemesterType.Fall   => "Fall",
                SemesterType.Spring => "Spring",
                _                   => "Summer"
            };

            var totalSemesters = (int)Math.Ceiling((double)creditsRequired / DefaultMaxCreditsPerSemester);

            return ($"{semLabel} {year}", totalSemesters);
        }

        private static (SemesterType Semester, int Year) AdvanceSemester(
            SemesterType semester, int year) => semester switch
        {
            SemesterType.Fall   => (SemesterType.Spring, year + 1),
            SemesterType.Spring => (SemesterType.Fall,   year),
            _                   => (SemesterType.Fall,   year + 1)
        };

        private static Dictionary<string, decimal> BuildDefaultGradeScale() =>
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["A+"] = 4.0m, ["A"]  = 4.0m, ["A-"] = 3.7m,
                ["B+"] = 3.3m, ["B"]  = 3.0m, ["B-"] = 2.7m,
                ["C+"] = 2.3m, ["C"]  = 2.0m, ["C-"] = 1.7m,
                ["D+"] = 1.3m, ["D"]  = 1.0m, ["D-"] = 0.7m,
                ["F"]  = 0.0m
            };
    }
}
