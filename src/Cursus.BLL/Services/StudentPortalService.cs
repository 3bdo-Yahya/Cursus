using Cursus.BLL.Interfaces;
using Cursus.DAL.Database;
using Cursus.Domain.DTOs;
using Cursus.Domain.Entities;
using Cursus.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Cursus.BLL.Services;

public class StudentPortalService : IStudentPortalService
{
    private static readonly IReadOnlyDictionary<string, double> GradePoints = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
    {
        ["A+"] = 4.00, ["A"] = 4.00, ["A-"] = 3.67,
        ["B+"] = 3.33, ["B"] = 3.00, ["B-"] = 2.67,
        ["C+"] = 2.33, ["C"] = 2.00, ["C-"] = 1.67,
        ["D+"] = 1.33, ["D"] = 1.00, ["F"] = 0.00,
    };

    private static readonly IReadOnlyList<string> GradeOrder =
    [
        "A+", "A", "A-",
        "B+", "B", "B-",
        "C+", "C", "C-",
        "D+", "D", "D-",
        "F"
    ];

    private readonly ApplicationDbContext _context;

    public StudentPortalService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<StudentPortalSnapshot?> GetSnapshotAsync(string studentId)
    {
        var user = await _context.Users
            .Include(u => u.Department)
            .Include(u => u.StudentCourses)
                .ThenInclude(sc => sc.Course)
            .Include(u => u.StandingHistories)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == studentId);

        if (user is null)
        {
            return null;
        }

        var departmentId = user.DepartmentId;
        if (departmentId is null)
        {
            return BuildMinimalSnapshot(user);
        }

        var graduationRequirements = await _context.GraduationRequirements
            .Include(gr => gr.GraduationRequirementCourses)
                .ThenInclude(grc => grc.Course)
            .Where(gr => gr.DepartmentId == departmentId)
            .AsNoTracking()
            .ToListAsync();

        var departmentCourses = await _context.Courses
            .Include(c => c.Department)
            .Include(c => c.Prerequisites)
                .ThenInclude(p => p.Prerequisite)
            .Where(c => c.DepartmentId == departmentId && c.IsActive)
            .AsNoTracking()
            .ToListAsync();

        return BuildSnapshot(user, graduationRequirements, departmentCourses);
    }

    private StudentPortalSnapshot BuildSnapshot(
        AppUser user,
        IReadOnlyList<GraduationRequirement> graduationRequirements,
        IReadOnlyList<Course> departmentCourses)
    {
        var department = user.Department!;
        var latestByCourse = GetLatestCourseRecords(user.StudentCourses);
        var passedCourseIds = latestByCourse.Values
            .Where(sc => sc.Status == StudentCourseStatus.Completed)
            .Select(sc => sc.CourseId)
            .ToHashSet();

        var orderedHistories = user.StandingHistories
            .OrderBy(h => h.AcademicYear)
            .ThenBy(h => h.Semester)
            .ToList();

        var latestHistory = orderedHistories.LastOrDefault();
        var previousHistory = orderedHistories.Count >= 2
            ? orderedHistories[^2]
            : null;

        var cgpa = latestHistory is null ? 0.0 : (double)latestHistory.CumulativeGpa;
        var lastSemesterGpa = latestHistory is null ? 0.0 : (double)latestHistory.SemesterGpa;
        var cgpaChange = latestHistory is null || previousHistory is null
            ? 0.0
            : (double)(latestHistory.CumulativeGpa - previousHistory.CumulativeGpa);

        var creditsEarned = user.StudentCourses
            .Where(sc => sc.Status == StudentCourseStatus.Completed)
            .Sum(sc => sc.Course?.CreditHours ?? 0);

        var creditsRequired = department.TotalCreditsRequired;
        var creditsRemaining = Math.Max(0, creditsRequired - creditsEarned);
        var minGpa = (double)department.MinGpaForGraduation;
        var isOverloadEligible = cgpa >= 3.0;

        var semesterLabel = BuildSemesterLabel(user.CurrentSemester, user.AcademicYear);
        var semestersCompleted = orderedHistories.Count;
        var totalSemesters = Math.Max(8, (int)Math.Ceiling(creditsRequired / 16.0) * 2);
        var yearLevel = ResolveYearLevel(user.AcademicYear, semestersCompleted);
        var standingLabel = MapStandingLabel(user.CurrentStanding);
        var displayName = user.DisplayName;
        var initials = GetInitials(displayName);
        var subtitle = $"{displayName} · {department.Name} · Year {yearLevel} · {semesterLabel}";

        var currentTerm = new AcademicTerm(user.AcademicYear ?? string.Empty, user.CurrentSemester);
        var graduationSemester = EstimateGraduationTerm(currentTerm, creditsRemaining, 15);
        var overloadGraduationSemester = isOverloadEligible
            ? EstimateGraduationTerm(currentTerm, creditsRemaining, 18)
            : graduationSemester;

        var progressCategories = BuildProgressCategories(
            graduationRequirements,
            latestByCourse,
            passedCourseIds,
            departmentCourses);

        var courseRemainingCounts = CountRemainingCourses(progressCategories);

        var currentCourses = user.StudentCourses
            .Where(sc => sc.Status == StudentCourseStatus.InProgress)
            .Select(sc => new StudentCurrentCourseDto(
                sc.Course?.Code ?? "—",
                sc.Course?.Name ?? "Unknown Course",
                $"{sc.Semester} · {BuildSemesterLabel(sc.Semester, sc.AcademicYear)}",
                sc.Course?.CreditHours ?? 0,
                sc.Course?.CourseType is CourseType.DeptElective or CourseType.FreeElective))
            .ToList();

        var simulatorCurrentCourses = currentCourses
            .Select(c => new SimulatorCourseDto(c.Code, c.Name, c.CreditHours))
            .ToList();

        var improvableCourses = latestByCourse.Values
            .Where(sc => sc.Status == StudentCourseStatus.Failed &&
                         !string.IsNullOrWhiteSpace(sc.Grade) &&
                         GetGradePoints(sc.Grade) <= GradePoints["D+"])
            .Select(sc => new ImprovableCourseDto(
                sc.Course?.Code ?? "—",
                sc.Course?.Name ?? "Unknown Course",
                sc.Course?.CreditHours ?? 0,
                sc.Grade!,
                GetGradePoints(sc.Grade!)))
            .ToList();

        var courseYears = ComputeCourseYears(departmentCourses);
        var courseMapNodes = departmentCourses
            .Select(course =>
            {
                latestByCourse.TryGetValue(course.Id, out var record);
                var status = ResolveCourseMapStatus(course, record, passedCourseIds);
                return new CourseMapNodeDto(
                    course.Code,
                    course.Name,
                    course.CreditHours,
                    MapCourseTypeLabel(course.CourseType),
                    MapAvailabilityLabel(course.SemesterAvailability),
                    GetDeptAbbrev(course),
                    course.PassingGradeThreshold,
                    status,
                    record?.Grade,
                    course.Prerequisites
                        .Select(p => p.Prerequisite?.Code ?? string.Empty)
                        .Where(code => !string.IsNullOrEmpty(code))
                        .ToList(),
                    courseYears.GetValueOrDefault(course.Id, 1));
            })
            .OrderBy(c => c.Year)
            .ThenBy(c => c.Id)
            .ToList();

        var completedQualityPoints = user.StudentCourses
            .Where(sc => sc.Status == StudentCourseStatus.Completed && !string.IsNullOrWhiteSpace(sc.Grade))
            .Sum(sc => GetGradePoints(sc.Grade!) * (sc.Course?.CreditHours ?? 0));

        var jsContext = BuildJsContext(
            user,
            department,
            yearLevel,
            semesterLabel,
            standingLabel,
            cgpa,
            creditsEarned,
            creditsRequired,
            graduationSemester,
            latestByCourse);

        var display = new StudentDisplayContextDto(
            displayName,
            initials,
            department.Name,
            yearLevel,
            semesterLabel,
            standingLabel,
            subtitle);

        var gpa = new StudentGpaStatsDto(
            cgpa,
            lastSemesterGpa,
            cgpaChange,
            minGpa,
            isOverloadEligible,
            Math.Round(completedQualityPoints, 2));

        var credits = new StudentCreditStatsDto(
            creditsEarned,
            creditsRequired,
            creditsRemaining,
            courseRemainingCounts.Total,
            courseRemainingCounts.Core,
            courseRemainingCounts.Elective);

        var graduation = new StudentGraduationEstimateDto(
            graduationSemester,
            overloadGraduationSemester,
            semestersCompleted,
            totalSemesters);

        return new StudentPortalSnapshot(
            display,
            gpa,
            credits,
            graduation,
            currentCourses,
            progressCategories,
            simulatorCurrentCourses,
            improvableCourses,
            courseMapNodes,
            jsContext);
    }

    private StudentPortalSnapshot BuildMinimalSnapshot(AppUser user)
    {
        var displayName = user.DisplayName;
        var semesterLabel = BuildSemesterLabel(user.CurrentSemester, user.AcademicYear);
        var standingLabel = MapStandingLabel(user.CurrentStanding);
        var yearLevel = ResolveYearLevel(user.AcademicYear, user.StandingHistories.Count);
        var subtitle = $"{displayName} · Year {yearLevel} · {semesterLabel}";

        var display = new StudentDisplayContextDto(
            displayName,
            GetInitials(displayName),
            user.Department?.Name ?? "Undeclared",
            yearLevel,
            semesterLabel,
            standingLabel,
            subtitle);

        var gpa = new StudentGpaStatsDto(0, 0, 0, 2.0, false, 0);
        var credits = new StudentCreditStatsDto(0, 0, 0, 0, 0, 0);
        var graduation = new StudentGraduationEstimateDto("—", "—", 0, 8);

        var jsContext = new StudentJsContextDto(
            displayName,
            display.Department,
            yearLevel,
            semesterLabel,
            0,
            standingLabel,
            0,
            0,
            "—",
            string.Empty,
            string.Empty,
            string.Empty);

        return new StudentPortalSnapshot(
            display,
            gpa,
            credits,
            graduation,
            [],
            [],
            [],
            [],
            [],
            jsContext);
    }

    private static StudentJsContextDto BuildJsContext(
        AppUser user,
        Department department,
        int yearLevel,
        string semesterLabel,
        string standingLabel,
        double cgpa,
        int creditsEarned,
        int creditsRequired,
        string graduationSemester,
        IReadOnlyDictionary<int, StudentCourse> latestByCourse)
    {
        var completed = latestByCourse.Values
            .Where(sc => sc.Status == StudentCourseStatus.Completed)
            .Select(sc => $"{sc.Course?.Code} ({sc.Grade ?? "—"})")
            .ToList();

        var inProgress = latestByCourse.Values
            .Where(sc => sc.Status == StudentCourseStatus.InProgress)
            .Select(sc => $"{sc.Course?.Code} {sc.Course?.Name}")
            .ToList();

        var failed = latestByCourse.Values
            .Where(sc => sc.Status == StudentCourseStatus.Failed)
            .Select(sc => $"{sc.Course?.Code} ({sc.Grade ?? "F"})")
            .ToList();

        return new StudentJsContextDto(
            user.DisplayName,
            department.Name,
            yearLevel,
            semesterLabel,
            cgpa,
            standingLabel,
            creditsEarned,
            creditsRequired,
            graduationSemester,
            string.Join(", ", completed),
            string.Join(", ", inProgress),
            string.Join(", ", failed));
    }

    private static IReadOnlyList<ProgressCategoryDto> BuildProgressCategories(
        IReadOnlyList<GraduationRequirement> graduationRequirements,
        IReadOnlyDictionary<int, StudentCourse> latestByCourse,
        IReadOnlySet<int> passedCourseIds,
        IReadOnlyList<Course> departmentCourses)
    {
        if (graduationRequirements.Count == 0)
        {
            return [];
        }

        return graduationRequirements
            .OrderBy(gr => gr.CategoryType)
            .Select(requirement =>
            {
                var styling = MapCategoryStyling(requirement.CategoryType);
                var (title, sub) = MapCategoryLabels(requirement.CategoryType);
                var requirementCourses = requirement.GraduationRequirementCourses
                    .Select(grc => grc.Course)
                    .Where(course => course is not null)
                    .Cast<Course>()
                    .OrderBy(course => course.Code)
                    .ToList();

                var courses = requirementCourses
                    .Select(course =>
                    {
                        latestByCourse.TryGetValue(course.Id, out var record);
                        var status = MapProgressCourseStatus(course, record, passedCourseIds);
                        var isLocked = status == "locked";
                        return new ProgressCourseDto(
                            course.Code,
                            isLocked ? $"{course.Name} (Locked)" : course.Name,
                            course.CreditHours,
                            record?.Grade,
                            status,
                            isLocked);
                    })
                    .ToList();

                var earnedCredits = courses
                    .Where(c => c.Status == "done")
                    .Sum(c => c.CreditHours);

                var percentage = requirement.RequiredCredits > 0
                    ? Math.Round((double)earnedCredits / requirement.RequiredCredits * 100, 1)
                    : 0;

                return new ProgressCategoryDto(
                    title,
                    sub,
                    styling.IconStyle,
                    styling.BarClass,
                    styling.BadgeClass,
                    requirement.RequiredCredits,
                    earnedCredits,
                    percentage,
                    courses);
            })
            .ToList();
    }

    private static (int Total, int Core, int Elective) CountRemainingCourses(
        IReadOnlyList<ProgressCategoryDto> categories)
    {
        var total = 0;
        var core = 0;
        var elective = 0;

        foreach (var category in categories)
        {
            var remaining = category.Courses.Count(c => c.Status is not "done" and not "progress");
            total += remaining;

            if (category.Name.StartsWith("Core", StringComparison.Ordinal))
            {
                core += remaining;
            }
            else if (category.Name.Contains("Elective", StringComparison.Ordinal))
            {
                elective += remaining;
            }
        }

        return (total, core, elective);
    }

    private static Dictionary<int, StudentCourse> GetLatestCourseRecords(IEnumerable<StudentCourse> records) =>
        records
            .GroupBy(sc => sc.CourseId)
            .ToDictionary(
                g => g.Key,
                g => g
                    .OrderByDescending(sc => sc.AcademicYear)
                    .ThenByDescending(sc => sc.Semester)
                    .First());

    private static string ResolveCourseMapStatus(
        Course course,
        StudentCourse? record,
        IReadOnlySet<int> passedCourseIds)
    {
        if (record?.Status == StudentCourseStatus.Completed)
        {
            return "passed";
        }

        if (record?.Status == StudentCourseStatus.InProgress)
        {
            return "in-progress";
        }

        if (record?.Status == StudentCourseStatus.Failed)
        {
            return "failed";
        }

        return ArePrereqsMet(course, passedCourseIds) ? "remaining" : "blocked";
    }

    private static string MapProgressCourseStatus(
        Course course,
        StudentCourse? record,
        IReadOnlySet<int> passedCourseIds)
    {
        return record?.Status switch
        {
            StudentCourseStatus.Completed => "done",
            StudentCourseStatus.InProgress => "progress",
            StudentCourseStatus.Failed => "failed",
            _ => ArePrereqsMet(course, passedCourseIds) ? "open" : "locked"
        };
    }

    private static bool ArePrereqsMet(Course course, IReadOnlySet<int> passedCourseIds) =>
        course.Prerequisites.All(p => passedCourseIds.Contains(p.PrerequisiteId));

    private static Dictionary<int, int> ComputeCourseYears(IReadOnlyList<Course> courses)
    {
        var courseById = courses.ToDictionary(c => c.Id);
        var memo = new Dictionary<int, int>();

        int ResolveYear(int courseId, HashSet<int> visiting)
        {
            if (memo.TryGetValue(courseId, out var cached))
            {
                return cached;
            }

            if (!courseById.TryGetValue(courseId, out var course))
            {
                return 1;
            }

            if (!visiting.Add(courseId))
            {
                return 1;
            }

            var depth = course.Prerequisites.Count == 0
                ? 0
                : course.Prerequisites.Max(p => ResolveYear(p.PrerequisiteId, visiting));

            visiting.Remove(courseId);
            var year = Math.Min(4, depth + 1);
            memo[courseId] = year;
            return year;
        }

        foreach (var course in courses)
        {
            ResolveYear(course.Id, []);
        }

        return memo;
    }

    private static string EstimateGraduationTerm(AcademicTerm currentTerm, int creditsRemaining, int creditsPerSemester)
    {
        if (creditsRemaining <= 0)
        {
            return FormatTerm(currentTerm);
        }

        var semestersNeeded = (int)Math.Ceiling(creditsRemaining / (double)creditsPerSemester);
        var term = currentTerm;

        for (var i = 0; i < semestersNeeded; i++)
        {
            term = term.Next();
        }

        return FormatTerm(term);
    }

    private static string FormatTerm(AcademicTerm term)
    {
        var year = ExtractCalendarYear(term.AcademicYear, term.Semester);
        return $"{term.Semester} {year}";
    }

    private static string BuildSemesterLabel(SemesterType semester, string? academicYear)
    {
        if (string.IsNullOrWhiteSpace(academicYear))
        {
            return semester.ToString();
        }

        if (int.TryParse(academicYear, out _))
        {
            return semester.ToString();
        }

        var year = ExtractCalendarYear(academicYear, semester);
        return $"{semester} {year}";
    }

    private static int ExtractCalendarYear(string academicYear, SemesterType semester)
    {
        var parts = academicYear.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 2 &&
            int.TryParse(parts[0], out var startYear) &&
            int.TryParse(parts[1], out var endYear))
        {
            return semester == SemesterType.Fall ? startYear : endYear;
        }

        return int.TryParse(academicYear, out var numericYear) ? numericYear : DateTime.UtcNow.Year;
    }

    private static int ResolveYearLevel(string? academicYear, int semestersCompleted)
    {
        if (!string.IsNullOrWhiteSpace(academicYear) &&
            int.TryParse(academicYear, out var numericYear))
        {
            return Math.Clamp(numericYear, 1, 6);
        }

        return Math.Clamp((semestersCompleted / 2) + 1, 1, 4);
    }

    private static string MapStandingLabel(AcademicStanding standing) => standing switch
    {
        AcademicStanding.Good => "Good Standing",
        AcademicStanding.Warning => "Academic Warning",
        AcademicStanding.Probation => "Academic Probation",
        AcademicStanding.Dismissed => "Dismissed",
        _ => standing.ToString()
    };

    private static (string IconStyle, string BarClass, string BadgeClass) MapCategoryStyling(CourseType type) => type switch
    {
        CourseType.Core => ("background:var(--icon-blue-bg);", "cat-bar-blue", "cr-badge-blue"),
        CourseType.DeptElective => ("background:var(--icon-purple-bg);", "cat-bar-purple", "cr-badge-purple"),
        CourseType.FreeElective => ("background:var(--icon-amber-bg);", "cat-bar-amber", "cr-badge-amber"),
        CourseType.UniversityReq => ("background:rgba(16,185,129,.12);", "cat-bar-green", "cr-badge-green"),
        _ => ("background:var(--icon-blue-bg);", "cat-bar-blue", "cr-badge-blue")
    };

    private static (string Title, string Subtitle) MapCategoryLabels(CourseType type) => type switch
    {
        CourseType.Core => ("Core Courses", "Mandatory foundation courses"),
        CourseType.DeptElective => ("Department Elective", "Choose from approved electives"),
        CourseType.FreeElective => ("Free Elective", "Any approved course university-wide"),
        CourseType.UniversityReq => ("University Requirements", "Required by all graduates"),
        _ => (type.ToString(), string.Empty)
    };

    private static string MapCourseTypeLabel(CourseType type) => type switch
    {
        CourseType.Core => "Core",
        CourseType.DeptElective => "Elective",
        CourseType.FreeElective => "Elective",
        CourseType.UniversityReq => "Univ. Req.",
        _ => type.ToString()
    };

    private static string MapAvailabilityLabel(SemesterAvailability availability) => availability switch
    {
        SemesterAvailability.Fall => "Fall",
        SemesterAvailability.Spring => "Spring",
        SemesterAvailability.FallSpring => "Fall & Spring",
        SemesterAvailability.All => "All",
        _ => availability.ToString()
    };

    private static string GetDeptAbbrev(Course course)
    {
        var letters = new string(course.Code.TakeWhile(char.IsLetter).ToArray());
        return string.IsNullOrWhiteSpace(letters) ? "Dept" : letters;
    }

    private static double GetGradePoints(string grade) =>
        GradePoints.TryGetValue(grade.Trim(), out var points) ? points : 0.0;

    private static string GetInitials(string name)
    {
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
        {
            return $"{char.ToUpper(parts[0][0])}{char.ToUpper(parts[1][0])}";
        }

        return name.Length >= 2 ? name[..2].ToUpper() : name.ToUpper();
    }

    private readonly record struct AcademicTerm(string AcademicYear, SemesterType Semester)
    {
        public AcademicTerm Next()
        {
            if (Semester == SemesterType.Fall)
            {
                return new AcademicTerm(AcademicYear, SemesterType.Spring);
            }

            if (Semester == SemesterType.Spring)
            {
                var parts = AcademicYear.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (parts.Length == 2 &&
                    int.TryParse(parts[0], out var startYear) &&
                    int.TryParse(parts[1], out var endYear))
                {
                    return new AcademicTerm($"{startYear + 1}-{endYear + 1}", SemesterType.Fall);
                }

                return new AcademicTerm(AcademicYear, SemesterType.Fall);
            }

            return new AcademicTerm(AcademicYear, SemesterType.Fall);
        }
    }
}
