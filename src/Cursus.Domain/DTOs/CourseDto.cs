using Cursus.Domain.Enums;

namespace Cursus.Domain.DTOs
{
    public record CourseDto(
        int Id,
        string Code,
        string Name,
        int CreditHours,
        CourseType CourseType,
        SemesterAvailability SemesterAvailability,
        string PassingGradeThreshold,
        int DepartmentId,
        bool IsActive,
        IEnumerable<CoursePrerequisiteDto> Prerequisites
    );
}