using Cursus.Domain.Enums;

namespace Cursus.Domain.DTOs
{
    public record CourseNodeDto(
        int Id,
        string Code,
        string Name,
        int CreditHours,
        StudentCourseStatus? Status,  // Color code: Completed, Failed, InProgress, or null
        string? Grade,
        CourseType CourseType,
        int? RecommendedSemester
    );
}