namespace Cursus.Domain.DTOs;

/// <summary>
/// Represents a course that becomes blocked (directly or transitively)
/// when a prerequisite course is simulated as failed.
/// </summary>
public record BlockedCourseDto(
    int CourseId,
    string Code,
    string Name,
    int CreditHours,
    /// <summary>
    /// BFS depth from the failed course.
    /// 1 = direct dependency, 2+ = chain (transitive) dependency.
    /// </summary>
    int Depth,
    string? NormalTermLabel = null,
    string? NewTermLabel = null
);
