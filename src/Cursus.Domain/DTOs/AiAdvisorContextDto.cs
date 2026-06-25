using Cursus.Domain.Enums;

namespace Cursus.Domain.DTOs;

/// <summary>
/// Academic data supplied to the AI advisor by the application layer.
/// </summary>
public sealed class AiAdvisorContextDto
{
    public string DisplayName { get; init; } = string.Empty;
    public string DepartmentName { get; init; } = string.Empty;
    public string AcademicYear { get; init; } = string.Empty;
    public SemesterType? CurrentSemester { get; init; }
    public AcademicStanding? AcademicStanding { get; init; }
    public decimal? Cgpa { get; init; }
    public decimal? MinGpaForGraduation { get; init; }
    public int? CreditsCompleted { get; init; }
    public int? CreditsRequired { get; init; }
    public int? CreditsRemaining { get; init; }
    public int? OverallProgressPercentage { get; init; }
    public bool? IsOverloadEligible { get; init; }
    public bool? IsOnTrack { get; init; }
    public string ProjectedGraduation { get; init; } = string.Empty;

    public IReadOnlyCollection<AiAdvisorCategoryProgressDto> CategoryProgress { get; init; } = [];
    public IReadOnlyCollection<AiAdvisorCourseDto> CompletedCourses { get; init; } = [];
    public IReadOnlyCollection<AiAdvisorCourseDto> InProgressCourses { get; init; } = [];
    public IReadOnlyCollection<AiAdvisorCourseDto> FailedOrLowGradeCourses { get; init; } = [];
    public IReadOnlyCollection<AiAdvisorCourseDto> AvailableCourses { get; init; } = [];
    public IReadOnlyCollection<AiAdvisorCourseDto> LockedCourses { get; init; } = [];
}

/// <summary>
/// Credit progress for one graduation requirement category.
/// </summary>
public sealed class AiAdvisorCategoryProgressDto
{
    public string Label { get; init; } = string.Empty;
    public int RequiredCredits { get; init; }
    public int EarnedCredits { get; init; }
    public int InProgressCredits { get; init; }
    public int Percentage { get; init; }
    public bool IsSatisfied { get; init; }
}

/// <summary>
/// Course information included in an advisor prompt.
/// </summary>
public sealed class AiAdvisorCourseDto
{
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int CreditHours { get; init; }
    public string? Grade { get; init; }
}

/// <summary>
/// One previous chat turn supplied to the AI advisor for continuity.
/// </summary>
public sealed class AiAdvisorMessageDto
{
    public string Role { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public DateTimeOffset? CreatedAtUtc { get; init; }
}
