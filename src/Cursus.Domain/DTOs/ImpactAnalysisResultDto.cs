namespace Cursus.Domain.DTOs
{
    /// <summary>
    /// Aggregate result of a fail-cascade simulation, including
    /// summary metrics consumed by course-map.js and impact-analyzer.js.
    /// </summary>
    public sealed record ImpactAnalysisResultDto(
        int FailedCourseId,
        string FailedCourseCode,
        string FailedCourseName,
        int FailedCourseCredits,
        IEnumerable<BlockedCourseDto> BlockedCourses,
        int BlockedCoursesCount,
        int CascadeDepth,
        int CreditsAtRisk,
        string Severity
    );
}