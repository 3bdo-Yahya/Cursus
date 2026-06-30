using Cursus.DAL.Database;
using Cursus.Domain.DTOs;
using Cursus.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Cursus.BLL.Services;

/// <summary>
/// Computes a full graduation audit by cross-referencing a student's
/// <see cref="Cursus.Domain.Entities.StudentCourse"/> records with the
/// department's <see cref="Cursus.Domain.Entities.GraduationRequirement"/>
/// catalogue, broken down by the four <see cref="CourseType"/> categories.
/// </summary>
public sealed class ProgressService : IProgressService
{
    private readonly ApplicationDbContext _db;
    private readonly IAcademicMetricsService _academicMetricsService;

    // Average credits per semester used for the graduation projection.
    private const int AvgCreditsPerSemester = 15;

    // Metadata for each category card in the UI.
    private static readonly Dictionary<CourseType, (string Label, string Description)> CategoryMeta =
        new()
        {
            [CourseType.Core]          = ("Core Courses",              "Mandatory foundation courses"),
            [CourseType.DeptElective]  = ("Department Elective",       "Choose from approved electives"),
            [CourseType.FreeElective]  = ("Free Elective",             "Any approved course university-wide"),
            [CourseType.UniversityReq] = ("University Requirements",   "Required by all graduates"),
        };

    public ProgressService(ApplicationDbContext db, IAcademicMetricsService academicMetricsService)
    {
        _db = db;
        _academicMetricsService = academicMetricsService;
    }

    // ── Public API ────────────────────────────────────────────────────────

    public async Task<GraduationAuditDto?> GetGraduationAuditAsync(string studentId)
    {
        // ── 1. Load student with all required navigations ─────────────────
        var student = await _db.Users
            .Include(u => u.Department)
                .ThenInclude(d => d!.University)
            .Include(u => u.StudentCourses)
                .ThenInclude(sc => sc.Course)
                    .ThenInclude(c => c!.Prerequisites)
            .Include(u => u.StandingHistories)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == studentId);

        if (student is null || student.DepartmentId is null || student.Department is null)
            return null;

        // ── 2. Load graduation requirements for the student's department ──
        var requirements = await _db.GraduationRequirements
            .Include(r => r.GraduationRequirementCourses)
                .ThenInclude(rc => rc.Course)
                    .ThenInclude(c => c!.Prerequisites)
            .AsNoTracking()
            .Where(r => r.DepartmentId == student.DepartmentId)
            .ToListAsync();

        var gradeScale = await _academicMetricsService.GetGradeScaleAsync(student.Department.UniversityId);
        var bestAttempts = _academicMetricsService.ResolveBestAttempts(student.StudentCourses);

        var completedCourseIds = bestAttempts
            .Where(sc => sc.Status == StudentCourseStatus.Completed)
            .Select(sc => sc.CourseId)
            .ToHashSet();

        var studentCourseMap = bestAttempts.ToDictionary(sc => sc.CourseId);

        var cgpa = _academicMetricsService.CalculateCgpa(bestAttempts, gradeScale);

        // ── 6. Build per-category progress ────────────────────────────────
        var categories = new List<CategoryProgressDto>();

        foreach (var courseType in new[] { CourseType.Core, CourseType.DeptElective,
                                           CourseType.FreeElective, CourseType.UniversityReq })
        {
            var requirement = requirements.FirstOrDefault(r => r.CategoryType == courseType);
            var (label, description) = CategoryMeta[courseType];

            CategoryProgressDto cat;

            if (requirement is null)
            {
                // No requirement defined for this category — treat as fully satisfied.
                cat = new CategoryProgressDto
                {
                    CourseType       = courseType,
                    Label            = label,
                    Description      = description,
                    RequiredCredits  = 0,
                    EarnedCredits    = 0,
                    InProgressCredits = 0,
                    Courses          = []
                };
            }
            else if (requirement.GraduationRequirementCourses.Count > 0)
            {
                // Explicit course list defined → audit course by course.
                cat = BuildExplicitCategoryProgress(
                    courseType, label, description,
                    requirement, studentCourseMap, completedCourseIds);
            }
            else
            {
                // No explicit list (e.g. FreeElective) → credit-hour counting only.
                cat = BuildCreditCountCategoryProgress(
                    courseType, label, description,
                    requirement, student.StudentCourses.ToList());
            }

            categories.Add(cat);
        }

        // ── 7. Compute totals across all categories ───────────────────────
        var totalEarned = categories.Sum(c => c.EarnedCredits);
        var totalRequired = student.Department.TotalCreditsRequired;

        // ── 8. Project graduation semester ────────────────────────────────
        var creditsRemaining = Math.Max(0, totalRequired - totalEarned);
        var graduationSemester = ProjectGraduationSemester(
            creditsRemaining, student.CurrentSemester, student.AcademicYear ?? "");

        // ── 9. Determine on-track status ──────────────────────────────────
        var isOnTrack = cgpa >= student.Department.MinGpaForGraduation;

        return new GraduationAuditDto
        {
            StudentId             = student.Id,
            StudentName           = student.DisplayName,
            DepartmentName        = student.Department.Name,
            AcademicYear          = student.AcademicYear ?? "N/A",
            CurrentSemester       = student.CurrentSemester,
            CurrentStanding       = student.CurrentStanding,
            TotalCreditsEarned    = totalEarned,
            TotalCreditsRequired  = totalRequired,
            Cgpa                  = cgpa,
            EstimatedGradSemester = graduationSemester,
            IsOnTrack             = isOnTrack,
            MinGpaForGraduation   = student.Department.MinGpaForGraduation,
            Categories            = categories
        };
    }

    // ── Private helpers ───────────────────────────────────────────────────

    /// <summary>
    /// Builds a <see cref="CategoryProgressDto"/> for a category that has an
    /// explicit list of eligible courses in <c>GraduationRequirementCourses</c>.
    /// Each course is individually classified as Completed/InProgress/Failed/Available/Locked.
    /// </summary>
    private CategoryProgressDto BuildExplicitCategoryProgress(
        CourseType courseType,
        string label,
        string description,
        Cursus.Domain.Entities.GraduationRequirement requirement,
        Dictionary<int, Cursus.Domain.Entities.StudentCourse> studentCourseMap,
        HashSet<int> completedCourseIds)
    {
        var auditItems = new List<CourseAuditItemDto>();
        int earnedCredits = 0;
        int inProgressCredits = 0;

        foreach (var rc in requirement.GraduationRequirementCourses)
        {
            var course = rc.Course;
            if (course is null) continue;

            studentCourseMap.TryGetValue(course.Id, out var studentCourse);

            var auditStatus = ResolveAuditStatus(course, studentCourse, completedCourseIds);

            if (auditStatus == CourseAuditStatus.Completed)
                earnedCredits += course.CreditHours;
            else if (auditStatus == CourseAuditStatus.InProgress)
                inProgressCredits += course.CreditHours;

            auditItems.Add(new CourseAuditItemDto
            {
                CourseId    = course.Id,
                Code        = course.Code,
                Name        = course.Name,
                CreditHours = course.CreditHours,
                Grade       = studentCourse?.Grade,
                Status      = auditStatus
            });
        }

        // Sort: Completed → InProgress → Failed → Available → Locked, then by code.
        auditItems.Sort((a, b) =>
        {
            var statusOrder = StatusSortKey(a.Status).CompareTo(StatusSortKey(b.Status));
            return statusOrder != 0 ? statusOrder : string.Compare(a.Code, b.Code, StringComparison.Ordinal);
        });

        return new CategoryProgressDto
        {
            CourseType        = courseType,
            Label             = label,
            Description       = description,
            RequiredCredits   = requirement.RequiredCredits,
            EarnedCredits     = earnedCredits,
            InProgressCredits = inProgressCredits,
            Courses           = auditItems
        };
    }

    /// <summary>
    /// Builds a <see cref="CategoryProgressDto"/> for a category that has NO
    /// explicit course list (e.g. FreeElective). Credits are counted from the
    /// student's own records for that <see cref="CourseType"/>.
    /// </summary>
    private CategoryProgressDto BuildCreditCountCategoryProgress(
        CourseType courseType,
        string label,
        string description,
        Cursus.Domain.Entities.GraduationRequirement requirement,
        List<Cursus.Domain.Entities.StudentCourse> studentCourses)
    {
        var relevantCourses = studentCourses
            .Where(sc => sc.Course?.CourseType == courseType);

        var bestAttempts = _academicMetricsService.ResolveBestAttempts(relevantCourses);

        var relevantCoursesMap = bestAttempts.ToDictionary(sc => sc.CourseId);

        int earnedCredits = bestAttempts
            .Where(sc => sc.Status == StudentCourseStatus.Completed && sc.Course is not null)
            .Sum(sc => sc.Course!.CreditHours);

        int inProgressCredits = bestAttempts
            .Where(sc => sc.Status == StudentCourseStatus.InProgress && sc.Course is not null)
            .Sum(sc => sc.Course!.CreditHours);

        var auditItems = bestAttempts
            .Where(sc => sc.Course is not null)
            .Select(sc => new CourseAuditItemDto
            {
                CourseId    = sc.CourseId,
                Code        = sc.Course!.Code,
                Name        = sc.Course.Name,
                CreditHours = sc.Course.CreditHours,
                Grade       = sc.Grade,
                Status      = sc.Status switch
                {
                    StudentCourseStatus.Completed  => CourseAuditStatus.Completed,
                    StudentCourseStatus.InProgress => CourseAuditStatus.InProgress,
                    _                              => CourseAuditStatus.Failed
                }
            })
            .OrderBy(c => StatusSortKey(c.Status))
            .ThenBy(c => c.Code)
            .ToList();

        return new CategoryProgressDto
        {
            CourseType        = courseType,
            Label             = label,
            Description       = description,
            RequiredCredits   = requirement.RequiredCredits,
            EarnedCredits     = earnedCredits,
            InProgressCredits = inProgressCredits,
            Courses           = auditItems
        };
    }

    /// <summary>
    /// Determines the <see cref="CourseAuditStatus"/> for a single course
    /// by looking at the student's best record for it and checking prerequisites.
    /// </summary>
    private static CourseAuditStatus ResolveAuditStatus(
        Cursus.Domain.Entities.Course course,
        Cursus.Domain.Entities.StudentCourse? studentCourse,
        HashSet<int> completedCourseIds)
    {
        if (studentCourse is null)
        {
            // Check if locked by unmet prerequisites.
            bool allPrereqsMet = course.Prerequisites
                .All(p => completedCourseIds.Contains(p.PrerequisiteId));

            return allPrereqsMet ? CourseAuditStatus.Available : CourseAuditStatus.Locked;
        }

        return studentCourse.Status switch
        {
            StudentCourseStatus.Completed  => CourseAuditStatus.Completed,
            StudentCourseStatus.InProgress => CourseAuditStatus.InProgress,
            _                              => CourseAuditStatus.Failed
        };
    }

    /// <summary>
    /// Projects the likely graduation semester by dividing remaining credits
    /// by the average credits per semester, then rounding up to the next
    /// standard semester boundary (Fall / Spring).
    /// </summary>
    private static string ProjectGraduationSemester(
        int creditsRemaining,
        SemesterType currentSemester,
        string academicYear)
    {
        if (creditsRemaining <= 0)
            return "This Semester";

        int semestersNeeded = (int)Math.Ceiling((double)creditsRemaining / AvgCreditsPerSemester);

        // Parse the start year from "YYYY-YYYY".
        int startYear = TryParseStartYear(academicYear);

        // Advance semester by semester from current position.
        var sem = currentSemester;
        var year = startYear;

        for (int i = 0; i < semestersNeeded; i++)
        {
            (sem, year) = AdvanceSemester(sem, year);
        }

        return $"{sem} {year}/{year + 1}";
    }

    private static (SemesterType Semester, int Year) AdvanceSemester(SemesterType current, int year)
    {
        return current switch
        {
            SemesterType.Fall   => (SemesterType.Spring, year),
            SemesterType.Spring => (SemesterType.Summer, year),
            SemesterType.Summer => (SemesterType.Fall,   year + 1),
            _                   => (SemesterType.Spring, year)
        };
    }

    private static int TryParseStartYear(string academicYear)
    {
        if (string.IsNullOrWhiteSpace(academicYear))
            return DateTime.UtcNow.Year;

        var part = academicYear.Split('-', '/')[0].Trim();
        return int.TryParse(part, out var y) ? y : DateTime.UtcNow.Year;
    }

    private static int StatusSortKey(CourseAuditStatus status) => status switch
    {
        CourseAuditStatus.Completed  => 0,
        CourseAuditStatus.InProgress => 1,
        CourseAuditStatus.Failed     => 2,
        CourseAuditStatus.Available  => 3,
        CourseAuditStatus.Locked     => 4,
        _                            => 5
    };
}
