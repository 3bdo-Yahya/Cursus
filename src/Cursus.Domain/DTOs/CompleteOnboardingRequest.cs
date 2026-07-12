using Cursus.Domain.Enums;

namespace Cursus.Domain.DTOs;

/// <summary>
/// Input for student self-service academic placement after registration.
/// </summary>
public sealed class CompleteOnboardingRequest
{
    public required string StudentId { get; init; }
    public required int UniversityId { get; init; }
    public required int DepartmentId { get; init; }
    public required SemesterType CurrentSemester { get; init; }
    public DateTime? EnrollmentDate { get; init; }
}
