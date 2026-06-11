namespace Cursus.Domain.DTOs
{
    public record DepartmentDto(
        int Id,
        string Name,
        int UniversityId,
        string UniversityName,
        int TotalCreditsRequired,
        decimal MinGpaForGraduation,
        bool IsActive
    );
}