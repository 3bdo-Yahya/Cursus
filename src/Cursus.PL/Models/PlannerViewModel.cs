using System.Collections.Generic;
using Cursus.Domain.Enums;

namespace Cursus.PL.Models;

public class PlannerViewModel
{
    public string StudentName { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public int Year { get; set; }
    public string Semester { get; set; } = string.Empty;
    public double CurrentCgpa { get; set; }
    public string AcademicStanding { get; set; } = string.Empty;
    public string StandingCssClass { get; set; } = string.Empty;
    public int CompletedCredits { get; set; }
    public int TotalCreditsRequired { get; set; }
    public int CreditLimit { get; set; }
    public int OverloadLimit { get; set; }
    public List<string> CompletedCourses { get; set; } = new();
    public List<SimulatedCourseViewModel> CurrentlyEnrolledCourses { get; set; } = new();
    public List<PlannerCourseViewModel> Catalog { get; set; } = new();
}

public class PlannerCourseViewModel
{
    public string Id { get; set; } = string.Empty; // Course code
    public string Name { get; set; } = string.Empty;
    public int Credits { get; set; }
    public string Type { get; set; } = string.Empty; // Core, Dept. Elective, Free Elective, University Req.
    public string TypeClass { get; set; } = string.Empty; // type-core, type-elec, type-free, type-univ
    public List<string> Prereqs { get; set; } = new(); // Course codes of prerequisites
}
