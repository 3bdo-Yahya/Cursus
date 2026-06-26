namespace Cursus.Domain.DTOs
{
    public record CourseGraphDto(
        IEnumerable<CourseNodeDto> Nodes,
        IEnumerable<CourseEdgeDto> Edges
    );
}