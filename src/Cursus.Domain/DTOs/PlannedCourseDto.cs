using Cursus.Domain.Enums;

namespace Cursus.Domain.DTOs;

public class PlannedCourseDto
{
    public int CourseId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int CreditHours { get; set; }
    public CourseType CourseType { get; set; }
    public string AcademicYear { get; set; } = string.Empty;
    public SemesterType Semester { get; set; }
}
