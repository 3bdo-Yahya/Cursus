using Cursus.Domain.Enums;

namespace Cursus.Domain.DTOs;

/// <summary>
/// Aggregate result of a fail-cascade simulation, including
/// summary metrics and graduation delay consumed by course-map.js
/// and impact-analyzer.js.
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
    string Severity,
    int GraduationDelaySemesters,
    int RetakeDelaySemesters,
    int RecoverySemesters,
    int MaxCreditsPerSemester,
    int SemestersAffected,
    string RetakeSemesterLabel,
    string ProjectedGraduationLabel,
    string OriginalGraduationLabel = "",
    decimal CurrentCgpa = 0m,
    decimal ProjectedCgpa = 0m,
    decimal CgpaDelta = 0m,
    AcademicStanding CurrentStanding = AcademicStanding.Good,
    AcademicStanding ProjectedStanding = AcademicStanding.Good,
    bool StandingWouldChange = false,
    FailureScenarioType? ScenarioType = null,
    string ScenarioSummary = "",
    IReadOnlyList<RecoverySemesterDto>? RecoverySchedule = null,
    IReadOnlyList<string>? Recommendations = null,
    IReadOnlyList<RecoveryCourseDto>? ReplacementCourses = null
);

