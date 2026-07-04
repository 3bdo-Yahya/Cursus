using Cursus.Domain.Enums;

namespace Cursus.PL.Models;

public class GpaSimulatorViewModel
{
    public required string StudentId { get; init; }
    public required string StudentName { get; init; }
    public required string Department { get; init; }
    public int Year { get; init; }
    public required string Semester { get; init; }

    public double CurrentCgpa { get; init; }
    public double LastSgpa { get; init; }
    public required string AcademicStanding { get; init; }
    public int CompletedCredits { get; init; }
    public double CompletedQp { get; init; }
    public int GpaHours { get; init; }
    public required string StandingCssClass { get; init; }
    public double MaxGpa { get; init; } = 4.0;

    public required List<SimulatedCourseViewModel> CurrentCourses { get; init; }
    public required List<ImprovableCourseViewModel> ImprovableCourses { get; init; }
    public required List<string> CompletedCourses { get; init; }
    public required Dictionary<string, double> GradeScale { get; init; }
}

public class SimulatedCourseViewModel
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public int Credits { get; init; }
    public bool IsRetake { get; set; }
    public double OriginalPoints { get; set; }
    public string OriginalGrade { get; set; } = string.Empty;
}

public class ImprovableCourseViewModel
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public int Credits { get; init; }
    public required string OriginalGrade { get; init; }
    public double OriginalPoints { get; init; }
    
}

