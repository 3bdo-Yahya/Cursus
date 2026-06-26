using Cursus.Domain.Enums;

namespace Cursus.Domain.DTOs;

public sealed class StudentDashboardDto
{
    public required string StudentId { get; init; }
    public required string DisplayName { get; init; }
    public required string DepartmentName { get; init; }
    public required string AcademicYear { get; init; }
    public required SemesterType CurrentSemester { get; init; }
    public required AcademicStanding Standing { get; init; }
    public bool HasAcademicRecords { get; init; }

    public decimal Cgpa { get; init; }
    public decimal Sgpa { get; init; }
    public decimal CgpaChange { get; init; }

    public int CreditsCompleted { get; init; }
    public int CreditsRequired { get; init; }
    public int CoursesRemaining { get; init; }
    public int CoreCoursesRemaining { get; init; }
    public int ElectiveCoursesRemaining { get; init; }
    public int UniReqCoursesRemaining { get; init; }

    public required string StandingAlert { get; init; }
    public required string ProjectedGraduation { get; init; }
    public int SemestersCompleted { get; init; }
    public int TotalSemesters { get; init; }

    public required IReadOnlyList<EnrolledCourseDto> CurrentCourses { get; init; }
}

public sealed class EnrolledCourseDto
{
    public required string Code { get; init; }
    public required string Name { get; init; }
    public int CreditHours { get; init; }
    public bool IsElective { get; init; }
}
