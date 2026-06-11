namespace Cursus.Domain.DTOs
{
    public record AdminDashboardDto(
        int TotalUniversities,
        int TotalGraduationRequirements,
        int TotalDepartments,
        int ActiveDepartments,
        int InactiveDepartments,
        int TotalCourses,
        int ActiveCourses,
        int InactiveCourses,
        int TotalStudents
    );
}