using Cursus.Domain.Enums;

namespace Cursus.Domain.DTOs;

/// <summary>
/// Top-level result of a graduation audit for a single student.
/// Returned by <see cref="Cursus.Domain.Interfaces.Services.IProgressService"/>.
/// </summary>
public sealed class GraduationAuditDto
{
    public required string StudentId            { get; init; }
    public required string StudentName          { get; init; }
    public required string DepartmentName       { get; init; }
    public required string AcademicYear         { get; init; }
    public required SemesterType CurrentSemester { get; init; }
    public required AcademicStanding CurrentStanding { get; init; }

    // ── Credit totals ────────────────────────────────────────────────────
    /// <summary>Credits from <c>Completed</c> courses only.</summary>
    public int TotalCreditsEarned   { get; init; }
    public int TotalCreditsRequired { get; init; }

    public int OverallPercentage =>
        TotalCreditsRequired > 0
            ? (int)Math.Min(100, Math.Round((double)TotalCreditsEarned / TotalCreditsRequired * 100))
            : 0;

    public int CreditsRemaining => Math.Max(0, TotalCreditsRequired - TotalCreditsEarned);

    // ── GPA ──────────────────────────────────────────────────────────────
    /// <summary>
    /// Cumulative GPA taken from the most recent <c>StandingHistory</c> record.
    /// </summary>
    public decimal Cgpa { get; init; }

    /// <summary>GPA threshold for overload eligibility (≥ 3.0).</summary>
    public bool IsOverloadEligible => Cgpa >= 3.0m;

    // ── Graduation projection ─────────────────────────────────────────────
    public required string EstimatedGradSemester { get; init; }

    /// <summary>
    /// <c>true</c> when every <see cref="CategoryProgressDto.IsSatisfied"/>
    /// is <c>true</c> and Cgpa ≥ department minimum.
    /// </summary>
    public bool IsOnTrack { get; init; }

    public required decimal MinGpaForGraduation { get; init; }

    // ── Per-category breakdown ────────────────────────────────────────────
    public required IReadOnlyList<CategoryProgressDto> Categories { get; init; }
}

/// <summary>
/// Progress data for one <see cref="CourseType"/> category
/// (Core, DeptElective, FreeElective, UniversityReq).
/// </summary>
public sealed class CategoryProgressDto
{
    public required CourseType CourseType   { get; init; }
    public required string     Label        { get; init; }
    public required string     Description  { get; init; }

    public int RequiredCredits  { get; init; }
    public int EarnedCredits    { get; init; }
    public int InProgressCredits { get; init; }

    public bool IsSatisfied => RequiredCredits == 0 || EarnedCredits >= RequiredCredits;

    public int Percentage =>
        RequiredCredits > 0
            ? (int)Math.Min(100, Math.Round((double)EarnedCredits / RequiredCredits * 100))
            : 100;

    public required IReadOnlyList<CourseAuditItemDto> Courses { get; init; }
}

/// <summary>
/// The audit state of a single course within a category.
/// </summary>
public sealed class CourseAuditItemDto
{
    public int    CourseId    { get; init; }
    public required string Code        { get; init; }
    public required string Name        { get; init; }
    public int    CreditHours { get; init; }

    /// <summary>The student's letter grade, or <c>null</c> when not yet graded.</summary>
    public string? Grade      { get; init; }

    public required CourseAuditStatus Status { get; init; }
}

/// <summary>
/// Computed display state of a course in the graduation audit.
/// </summary>
public enum CourseAuditStatus
{
    /// <summary>Student has a passing grade for this course.</summary>
    Completed,

    /// <summary>Student is currently enrolled (InProgress status).</summary>
    InProgress,

    /// <summary>Student failed the course — does NOT count toward earned credits.</summary>
    Failed,

    /// <summary>
    /// Course is not yet taken and all prerequisites have been met
    /// (or the course has no prerequisites).
    /// </summary>
    Available,

    /// <summary>
    /// Course is not yet taken and at least one prerequisite is not yet Completed.
    /// </summary>
    Locked
}
