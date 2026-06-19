using Cursus.Domain.DTOs;

namespace Cursus.BLL.Services;

/// <summary>
/// Converts the authoritative graduation audit into the profile supplied
/// to the AI advisor.
/// </summary>
public static class AiAdvisorContextFactory
{
    private static readonly HashSet<string> LowGrades =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "C-",
            "D+",
            "D",
            "F"
        };

    public static AiAdvisorContextDto Create(GraduationAuditDto audit)
    {
        ArgumentNullException.ThrowIfNull(audit);

        var courses = audit.Categories
            .SelectMany(category => category.Courses)
            .GroupBy(course => course.CourseId)
            .Select(group => group
                .OrderBy(course => CourseStatusPriority(course.Status))
                .First())
            .ToList();

        return new AiAdvisorContextDto
        {
            DisplayName = audit.StudentName,
            DepartmentName = audit.DepartmentName,
            AcademicYear = audit.AcademicYear,
            CurrentSemester = audit.CurrentSemester,
            AcademicStanding = audit.CurrentStanding,
            Cgpa = audit.Cgpa,
            CreditsCompleted = audit.TotalCreditsEarned,
            CreditsRequired = audit.TotalCreditsRequired,
            ProjectedGraduation = audit.EstimatedGradSemester,
            CategoryProgress = audit.Categories
                .Select(category => new AiAdvisorCategoryProgressDto
                {
                    Label = category.Label,
                    RequiredCredits = category.RequiredCredits,
                    EarnedCredits = category.EarnedCredits,
                    InProgressCredits = category.InProgressCredits,
                    Percentage = category.Percentage,
                    IsSatisfied = category.IsSatisfied
                })
                .ToList(),
            CompletedCourses = MapCourses(
                courses.Where(course => course.Status == CourseAuditStatus.Completed)),
            InProgressCourses = MapCourses(
                courses.Where(course => course.Status == CourseAuditStatus.InProgress)),
            FailedOrLowGradeCourses = MapCourses(
                courses.Where(course =>
                    course.Status == CourseAuditStatus.Failed ||
                    IsLowGrade(course.Grade)))
        };
    }

    private static IReadOnlyCollection<AiAdvisorCourseDto> MapCourses(
        IEnumerable<CourseAuditItemDto> courses) =>
        courses
            .OrderBy(course => course.Code, StringComparer.OrdinalIgnoreCase)
            .Select(course => new AiAdvisorCourseDto
            {
                Code = course.Code,
                Name = course.Name,
                CreditHours = course.CreditHours,
                Grade = course.Grade
            })
            .ToList();

    private static bool IsLowGrade(string? grade) =>
        !string.IsNullOrWhiteSpace(grade) &&
        LowGrades.Contains(grade.Trim());

    private static int CourseStatusPriority(CourseAuditStatus status) => status switch
    {
        CourseAuditStatus.Completed => 0,
        CourseAuditStatus.InProgress => 1,
        CourseAuditStatus.Failed => 2,
        CourseAuditStatus.Available => 3,
        CourseAuditStatus.Locked => 4,
        _ => 5
    };
}
