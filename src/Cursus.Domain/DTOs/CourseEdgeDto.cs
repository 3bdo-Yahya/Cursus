namespace Cursus.Domain.DTOs
{
    public record CourseEdgeDto(
        int SourceCourseId,
        int TargetCourseId,
        string SourceCode,
        string TargetCode
    );
}