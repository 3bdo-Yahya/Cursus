namespace Cursus.Domain.DTOs
{
    public record CoursePrerequisiteDto(
        int PrerequisiteId,
        string PrerequisiteCode,
        string PrerequisiteName
    );
}