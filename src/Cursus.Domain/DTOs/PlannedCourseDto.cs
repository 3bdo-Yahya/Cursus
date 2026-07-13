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

public class PlanningTermDto
{
    public string AcademicYear { get; set; } = string.Empty;
    public SemesterType Semester { get; set; }
    public bool IsPrimary { get; set; }
}

public class PlannerTermCapacityDto
{
    public string AcademicYear { get; set; } = string.Empty;
    public SemesterType Semester { get; set; }
    public int ForcedInProgressCredits { get; set; }
    public int PlannedCredits { get; set; }
    public int RemainingRoom { get; set; }
}

