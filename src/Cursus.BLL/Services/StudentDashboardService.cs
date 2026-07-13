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
    private readonly IAcademicMetricsService _academicMetricsService;

    public StudentDashboardService(ApplicationDbContext db, IAcademicMetricsService academicMetricsService)
    {
        _db = db;
        _academicMetricsService = academicMetricsService;
    }

    public async Task<StudentDashboardDto?> GetDashboardDataAsync(string studentId)
    {
        var student = await _db.Users
            .Include(u => u.University)
            .Include(u => u.Department)
            .Include(u => u.StudentCourses)
                .ThenInclude(sc => sc.Course)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == studentId);

        if (student is null)
            return null;

        var gradeScale = await _academicMetricsService.GetGradeScaleAsync(student.Department?.UniversityId);
        var allCourses = student.StudentCourses.ToList();

        var bestAttempts = _academicMetricsService.ResolveBestAttempts(allCourses);

        var completedCourses = bestAttempts
            .Where(sc => sc.Status == StudentCourseStatus.Completed && sc.Course is not null)
            .ToList();

        var cgpa = _academicMetricsService.CalculateCgpa(bestAttempts, gradeScale);
        var termGpas = _academicMetricsService.CalculateSgpaByTerm(allCourses, gradeScale);

        // Trend: compare CGPA of last two graded terms
        var latestGraded = _academicMetricsService.GetLatestGradedTerms(termGpas, 2);
        decimal sgpa;
        decimal cgpaChange;

        if (latestGraded.Count == 0)
        {
            sgpa = 0m;
            cgpaChange = 0m;
        }
        else if (latestGraded.Count == 1)
        {
            sgpa = latestGraded[0].SemesterGpa;
            cgpaChange = latestGraded[0].CumulativeGpa; // change from 0
        }
        else
        {
            sgpa = latestGraded[^1].SemesterGpa;
            cgpaChange = Math.Round(latestGraded[^1].CumulativeGpa - latestGraded[^2].CumulativeGpa, 2);
        }
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
                    int earnedCredits = bestAttempts
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
                .Where(c => c.DepartmentId == student.DepartmentId && c.IsActive)
                .AsNoTracking()
                .ToListAsync();

            coreRemaining = fallbackCourses.Count(c => c.CourseType == CourseType.Core && !completedCourseIds.Contains(c.Id));
            electiveRemaining = fallbackCourses.Count(c => c.CourseType is CourseType.DeptElective or CourseType.FreeElective && !completedCourseIds.Contains(c.Id));
            uniReqRemaining = fallbackCourses.Count(c => c.CourseType == CourseType.UniversityReq && !completedCourseIds.Contains(c.Id));
        }

        var coursesRemaining = coreRemaining + electiveRemaining + uniReqRemaining;
        var standingAlert = BuildStandingAlert(student.CurrentStanding, cgpa);
        var maxCredits = _academicMetricsService.GetCreditLimits(student.CurrentStanding, cgpa);

        // Enrollment date: use EnrollmentDate property; fallback only for calendar academic years on courses
        var enrollmentDate = student.EnrollmentDate;
        if (enrollmentDate is null && allCourses.Count > 0)
        {
            var earliest = allCourses
                .OrderBy(sc => sc.AcademicYear)
                .ThenBy(sc => sc.Semester)
                .FirstOrDefault();

            if (earliest is not null
                && TryParseCalendarAcademicYearStart(earliest.AcademicYear, out var calYear))
            {
                var month = earliest.Semester == SemesterType.Spring ? 1
                    : earliest.Semester == SemesterType.Summer ? 6 : 9;
                enrollmentDate = new DateTime(calYear, month, 1);
            }
        }

        var enrollmentDateDisplay = enrollmentDate?.ToString("MMMM yyyy") ?? "—";

        var (projectedGraduation, totalSemesters) = ProjectGraduation(
            student.CurrentSemester, enrollmentDate, student.AcademicYear,
            creditsCompleted, creditsRequired, maxCredits);

        var semestersCompleted = termGpas
            .Count(t => t.Semester != SemesterType.Summer && t.GradedCredits > 0);
        semestersCompleted = Math.Min(semestersCompleted, totalSemesters);

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

        var gpaHistory = termGpas
            .Select(h => new GpaHistoryPointDto
            {
                SemLabel = h.SemLabel,
                Sgpa = h.SemesterGpa
            })
            .ToList();

        var gradedTerms = termGpas.Where(t => t.GradedCredits > 0).ToList();
        var highestSgpa = gradedTerms.Count > 0
            ? gradedTerms.Max(h => h.SemesterGpa)
            : 0m;

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
            SemestersCompleted = semestersCompleted,
            TotalSemesters = totalSemesters,
            CurrentCourses = currentCourses,
            UniversityName = student.University?.Name ?? "Not assigned",
            EnrollmentDate = enrollmentDateDisplay,
            HighestSgpa = highestSgpa,
            GpaHistory = gpaHistory
        };
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
        DateTime? enrollmentDate,
        string? academicYearOrdinal,
        int creditsCompleted,
        int creditsRequired,
        int maxCreditsPerSemester)
    {
        if (creditsRequired <= 0)
            return ("N/A", 0);

        var remaining = Math.Max(0, creditsRequired - creditsCompleted);
        if (remaining == 0)
            return ("Completed", 0);

        // Count semesters needed (Fall/Spring only, skip Summer)
        var semestersNeeded = (int)Math.Ceiling((double)remaining / maxCreditsPerSemester);
        var totalSemesters = (int)Math.Ceiling((double)creditsRequired / DefaultMaxCreditsPerSemester);

        // Anchor to calendar year from EnrollmentDate; fallback to current year
        var calendarYear = enrollmentDate?.Year ?? DateTime.UtcNow.Year;
        var currentOrdinal = int.TryParse(academicYearOrdinal, out var ord) ? ord : 1;

        // Determine current calendar year from enrollment + ordinal
        // e.g. enrolled Sep 2022, Year 3 → current academic year starts Sep 2024
        var currentAcademicStartYear = calendarYear + (currentOrdinal - 1);

        var semester = currentSemester;
        var year = semester switch
        {
            SemesterType.Fall => currentAcademicStartYear,
            _ => currentAcademicStartYear + 1
        };

        // Advance only Fall→Spring (skip Summer for graduation projection)
        for (var i = 0; i < semestersNeeded; i++)
        {
            (semester, year) = AdvanceSemesterNoSummer(semester, year);
        }

        var gradYear = currentOrdinal + (int)Math.Ceiling(semestersNeeded / 2.0);

        return ($"{semester} {year} (Year {gradYear})", totalSemesters);
    }

    private static bool TryParseCalendarAcademicYearStart(string? academicYear, out int startYear)
    {
        startYear = 0;
        if (string.IsNullOrWhiteSpace(academicYear))
            return false;

        var part = academicYear.Split('-', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return part?.Length == 4
            && int.TryParse(part, out startYear)
            && startYear >= 1900;
    }

    private static (SemesterType semester, int year) AdvanceSemesterNoSummer(SemesterType semester, int year)
    {
        return semester switch
        {
            SemesterType.Fall => (SemesterType.Spring, year + 1),
            _ => (SemesterType.Fall, year) // Spring → Fall (skip Summer)
        };
    }
}



