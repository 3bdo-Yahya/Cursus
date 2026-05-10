using Cursus.Domain.Enums;

namespace Cursus.BLL;

public sealed class StudentListItemDto
{
    public required string StudentId { get; init; }

    public required string FullName { get; init; }

    public required string Email { get; init; }

    public int? DepartmentId { get; init; }

    public string? DepartmentName { get; init; }

    public string? AcademicYear { get; init; }

    public SemesterType CurrentSemester { get; init; }

    public AcademicStanding CurrentStanding { get; init; }
}

public sealed class StudentDetailDto
{
    public required string StudentId { get; init; }

    public required string FullName { get; init; }

    public required string Email { get; init; }

    public int? DepartmentId { get; init; }

    public string? DepartmentName { get; init; }

    public string? AcademicYear { get; init; }

    public SemesterType CurrentSemester { get; init; }

    public AcademicStanding CurrentStanding { get; init; }

    public IReadOnlyList<StudentCourseRecordDto> CourseRecords { get; init; } = [];
}

public sealed class StudentCourseRecordDto
{
    public required int RecordId { get; init; }

    public required int CourseId { get; init; }

    public required string CourseCode { get; init; }

    public required string CourseName { get; init; }

    public required int CreditHours { get; init; }

    public string? Grade { get; init; }

    public StudentCourseStatus Status { get; init; }

    public SemesterType Semester { get; init; }

    public required string AcademicYear { get; init; }
}

public enum StudentCourseMutationError
{
    None = 0,
    StudentNotFound = 1,
    CourseNotFound = 2,
    RecordNotFound = 3,
    DuplicateRecord = 4,
    InvalidGrade = 5,
    InvalidAcademicYear = 6,
    CourseNotInStudentDepartment = 7
}

public sealed class StudentCourseMutationResult
{
    public bool Succeeded { get; init; }

    public StudentCourseMutationError Error { get; init; } = StudentCourseMutationError.None;

    public string? Message { get; init; }

    public string? StudentId { get; init; }

    public int? RecordId { get; init; }
}
