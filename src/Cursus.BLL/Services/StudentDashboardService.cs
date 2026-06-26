using Cursus.DAL.Database;
using Cursus.Domain.DTOs;
using Cursus.Domain.Entities;
using Cursus.Domain.Enums;
using Cursus.Domain.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace Cursus.BLL.Services;

public sealed class StudentDashboardService : IStudentDashboardService
{
    private const int DefaultMaxCreditsPerSemester = 18;
    private const int AlertCgpaThreshold = 2;
    private readonly ApplicationDbContext _db;

    public StudentDashboardService(ApplicationDbContext db) => _db = db;

    public async Task<StudentDashboardDto?> GetDashboardDataAsync(string studentId)
    {
        var student = await _db.Users
            .Include(u => u.Department)
            .Include(u => u.StudentCourses)
                .ThenInclude(sc => sc.Course)
            .Include(u => u.StandingHistories)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == studentId);

        if (student is null)
            return null;

        var gradeScale = await BuildGradeScaleAsync(student.Department?.UniversityId);
        var allCourses = student.StudentCourses.ToList();

        // Group by CourseId to filter out duplicates / retakes (Completed > InProgress > Failed)
        var studentCourseMap = allCourses
            .GroupBy(sc => sc.CourseId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(sc => sc.Status switch
                {
                    StudentCourseStatus.Completed => 0,
                    StudentCourseStatus.Failed => 1,
                    StudentCourseStatus.InProgress => 2,
                    _ => 3
                }).First());

        var completedCourses = studentCourseMap.Values
            .Where(sc => sc.Status == StudentCourseStatus.Completed && sc.Course is not null)
            .ToList();

        var gradedCourses = studentCourseMap.Values
            .Where(sc => sc.Status is StudentCourseStatus.Completed or StudentCourseStatus.Failed && !string.IsNullOrWhiteSpace(sc.Grade) && sc.Course is not null)
            .ToList();

        var cgpa = CalculateGpa(gradedCourses, gradeScale);
        var sgpa = CalculateSemesterGpa(student.StandingHistories, student.CurrentSemester, student.AcademicYear);
        var lastCgpa = student.StandingHistories
            .OrderByDescending(h => h.AcademicYear)
            .ThenByDescending(h => h.Semester)
            .FirstOrDefault()?.CumulativeGpa ?? cgpa;

        var cgpaChange = Math.Round(cgpa - lastCgpa, 2);
        var creditsCompleted = completedCourses.Sum(sc => sc.Course!.CreditHours);
        var creditsRequired = student.Department?.TotalCreditsRequired ?? 0;

        var completedCourseIds = completedCourses
            .Select(sc => sc.CourseId)
            .ToHashSet();

        // Load graduation requirements for this student's department
        var gradReqs = student.DepartmentId is null
            ? new List<GraduationRequirement>()
            : await _db.GraduationRequirements
                .Include(r => r.GraduationRequirementCourses)
                .Where(r => r.DepartmentId == student.DepartmentId)
                .AsNoTracking()
                .ToListAsync();

        int coreRemaining = 0;
        int electiveRemaining = 0;
        int uniReqRemaining = 0;

        if (student.DepartmentId is null)
        {
            // No department assigned yet 
        }
        else if (gradReqs.Count > 0)
        {
            foreach (var req in gradReqs)
            {
                if (req.CategoryType == CourseType.Core)
                {
                    coreRemaining = req.GraduationRequirementCourses
                        .Count(rc => !completedCourseIds.Contains(rc.CourseId));
                }
                else if (req.CategoryType is CourseType.DeptElective or CourseType.FreeElective or CourseType.UniversityReq)
                {
                    int earnedCredits = studentCourseMap.Values
                        .Where(sc => sc.Status == StudentCourseStatus.Completed && sc.Course?.CourseType == req.CategoryType)
                        .Sum(sc => sc.Course!.CreditHours);

                    int remainingCredits = Math.Max(0, req.RequiredCredits - earnedCredits);
                    int coursesNeeded = (int)Math.Ceiling(remainingCredits / 3.0);

                    if (req.CategoryType == CourseType.UniversityReq)
                        uniReqRemaining += coursesNeeded;
                    else
                        electiveRemaining += coursesNeeded;
                }
            }
        }
        else
        {
            // Fallback if graduation requirements are not seeded/defined for department
            var fallbackCourses = await _db.Courses
                .Where(c => (c.DepartmentId == student.DepartmentId || c.CourseType == CourseType.UniversityReq) && c.IsActive)
                .AsNoTracking()
                .ToListAsync();

            coreRemaining = fallbackCourses.Count(c => c.CourseType == CourseType.Core && !completedCourseIds.Contains(c.Id));
            electiveRemaining = fallbackCourses.Count(c => c.CourseType is CourseType.DeptElective or CourseType.FreeElective && !completedCourseIds.Contains(c.Id));
            uniReqRemaining = fallbackCourses.Count(c => c.CourseType == CourseType.UniversityReq && !completedCourseIds.Contains(c.Id));
        }

        var coursesRemaining = coreRemaining + electiveRemaining + uniReqRemaining;
        var standingAlert = BuildStandingAlert(student.CurrentStanding, cgpa);
        var (projectedGraduation, totalSemesters) = ProjectGraduation(student.CurrentSemester, student.AcademicYear, creditsCompleted, creditsRequired, student.CurrentStanding, cgpa);

        var currentCourses = allCourses
            .Where(sc => sc.Status == StudentCourseStatus.InProgress && sc.Course is not null)
            .Select(sc => new EnrolledCourseDto
            {
                Code = sc.Course!.Code,
                Name = sc.Course.Name,
                CreditHours = sc.Course.CreditHours,
                IsElective = sc.Course.CourseType is CourseType.DeptElective or CourseType.FreeElective
            })
            .ToList();

        return new StudentDashboardDto
        {
            StudentId = student.Id,
            DisplayName = student.DisplayName,
            DepartmentName = student.Department?.Name ?? "Not assigned",
            AcademicYear = student.AcademicYear ?? string.Empty,
            CurrentSemester = student.CurrentSemester,
            Standing = student.CurrentStanding,
            Cgpa = cgpa,
            Sgpa = sgpa,
            CgpaChange = cgpaChange,
            CreditsCompleted = creditsCompleted,
            CreditsRequired = creditsRequired,
            CoursesRemaining = coursesRemaining,
            CoreCoursesRemaining = coreRemaining,
            ElectiveCoursesRemaining = electiveRemaining,
            UniReqCoursesRemaining = uniReqRemaining,
            StandingAlert = standingAlert,
            HasAcademicRecords = allCourses.Count > 0,
            ProjectedGraduation = projectedGraduation,
            SemestersCompleted = student.StandingHistories.Count,
            TotalSemesters = totalSemesters,
            CurrentCourses = currentCourses
        };
    }

    private async Task<Dictionary<string, decimal>> BuildGradeScaleAsync(int? universityId)
    {
        if (universityId is null)
            return BuildDefaultGradeScale();

        var gradeScale = await _db.GradeScales
            .AsNoTracking()
            .Where(gs => gs.UniversityId == universityId)
            .ToDictionaryAsync(gs => gs.LetterGrade.ToUpper(), gs => gs.PointValue);

        return gradeScale.Count > 0 ? gradeScale : BuildDefaultGradeScale();
    }

    private static Dictionary<string, decimal> BuildDefaultGradeScale()
        => new()
        {
            ["A+"] = 4.0m,
            ["A"] = 4.0m,
            ["A-"] = 3.7m,
            ["B+"] = 3.3m,
            ["B"] = 3.0m,
            ["B-"] = 2.7m,
            ["C+"] = 2.3m,
            ["C"] = 2.0m,
            ["C-"] = 1.7m,
            ["D+"] = 1.3m,
            ["D"] = 1.0m,
            ["F"] = 0.0m
        };

    private static decimal CalculateGpa(IEnumerable<StudentCourse> records, Dictionary<string, decimal> gradeScale)
    {
        var totalPoints = 0m;
        var totalCredits = 0;

        foreach (var record in records)
        {
            if (record.Course is null || string.IsNullOrWhiteSpace(record.Grade))
                continue;

            var gradeKey = record.Grade.Trim().ToUpper();
            if (!gradeScale.TryGetValue(gradeKey, out var points))
                continue;

            totalPoints += points * record.Course.CreditHours;
            totalCredits += record.Course.CreditHours;
        }

        return totalCredits == 0 ? 0m : Math.Round(totalPoints / totalCredits, 2);
    }

    private static decimal CalculateSemesterGpa(IEnumerable<StandingHistory> histories, SemesterType currentSemester, string? academicYear)
    {
        var current = histories
            .Where(h => string.Equals(h.AcademicYear, academicYear, StringComparison.OrdinalIgnoreCase)
                        && h.Semester == currentSemester)
            .OrderByDescending(h => h.Id)
            .FirstOrDefault();

        return current?.SemesterGpa ?? 0m;
    }

    private static string BuildStandingAlert(AcademicStanding standing, decimal cgpa)
    {
        return standing switch
        {
            AcademicStanding.Probation => "Academic probation: immediate advisor support required.",
            AcademicStanding.Warning => cgpa < AlertCgpaThreshold
                ? "Academic warning: your GPA is below the safe threshold."
                : "Academic warning: maintain your current pace.",
            AcademicStanding.Dismissed => "Academic dismissal: contact the registrar immediately.",
            _ => cgpa >= 3.0m
                ? "You are in good standing and eligible for course overload."
                : "You are in good standing, but monitor your GPA closely."
        };
    }

    private static (string projectedGraduation, int totalSemesters) ProjectGraduation(
        SemesterType currentSemester,
        string? academicYear,
        int creditsCompleted,
        int creditsRequired,
        AcademicStanding standing,
        decimal cgpa)
    {
        if (creditsRequired <= 0)
            return ("N/A", 0);

        var remaining = Math.Max(0, creditsRequired - creditsCompleted);
        if (remaining == 0)
            return ("Completed", 0);

        var maxCredits = standing switch
        {
            AcademicStanding.Probation => 12,
            AcademicStanding.Warning => 15,
            _ => cgpa >= 3.0m ? 21 : DefaultMaxCreditsPerSemester
        };

        var semestersNeeded = (int)Math.Ceiling((double)remaining / maxCredits);
        var totalSemesters = (int)Math.Ceiling((double)creditsRequired / DefaultMaxCreditsPerSemester);

        var yearStart = ParseAcademicYearStart(academicYear);
        var semester = currentSemester;
        var year = currentSemester switch
        {
            SemesterType.Fall => yearStart,
            _ => yearStart + 1
        };

        for (var i = 0; i < semestersNeeded; i++)
        {
            (semester, year) = AdvanceSemester(semester, year);
        }

        return ($"{semester} {year}", totalSemesters);
    }

    private static int ParseAcademicYearStart(string? academicYear)
    {
        if (string.IsNullOrWhiteSpace(academicYear))
            return DateTime.UtcNow.Year;

        var part = academicYear.Split('-', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return int.TryParse(part, out var year) ? year : DateTime.UtcNow.Year;
    }

    private static (SemesterType semester, int year) AdvanceSemester(SemesterType semester, int year)
    {
        return semester switch
        {
            SemesterType.Fall => (SemesterType.Spring, year + 1),
            SemesterType.Spring => (SemesterType.Summer, year),
            _ => (SemesterType.Fall, year)
        };
    }
}
