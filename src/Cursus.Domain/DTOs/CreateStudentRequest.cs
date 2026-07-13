using Cursus.Domain.Enums;

namespace Cursus.Domain.DTOs;

/// <summary>
/// Input for admin-created student accounts (Identity + academic profile).
/// </summary>
public sealed class CreateStudentRequest
{
    public required string Email { get; init; }
    public required string Password { get; init; }
    public required int DepartmentId { get; init; }
    public required string AcademicYear { get; init; }
    public required SemesterType CurrentSemester { get; init; }
    public DateTime? EnrollmentDate { get; init; }
}
