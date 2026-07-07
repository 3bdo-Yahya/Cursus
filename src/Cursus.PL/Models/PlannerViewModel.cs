using System.Collections.Generic;
using Cursus.Domain.Enums;

namespace Cursus.PL.Models;

public class PlannerViewModel
{
    public string StudentId { get; set; } = string.Empty;
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
    public bool IsOverloadEligible { get; set; }
    public List<string> CompletedCourses { get; set; } = new();
    public List<string> InProgressCourses { get; set; } = new();
    public List<PlannerEnrolledCourseViewModel> CurrentlyEnrolledCourses { get; set; } = new();
    public List<PlannerTermViewModel> Terms { get; set; } = new();
    public PlannerTermCapacityViewModel PrimaryTermCapacity { get; set; } = new();
    public List<PlannerPlannedCourseViewModel> PlannedCourses { get; set; } = new();
    public List<PlannerCourseViewModel> Catalog { get; set; } = new();
}

public class PlannerTermViewModel
{
    public string AcademicYear { get; set; } = string.Empty;
    public SemesterType Semester { get; set; }
    public string Label { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
}

public class PlannerTermCapacityViewModel
{
    public string AcademicYear { get; set; } = string.Empty;
    public SemesterType Semester { get; set; }
    public int ForcedInProgressCredits { get; set; }
    public int PlannedCredits { get; set; }
    public int RemainingRoom { get; set; }
}

public class PlannerPlannedCourseViewModel
{
    public int CourseId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Credits { get; set; }
}

public class PlannerEnrolledCourseViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Credits { get; set; }
    public string Type { get; set; } = string.Empty;
    public string TypeClass { get; set; } = string.Empty;
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

public class PlannerCourseMutationRequest
{
    public int CourseId { get; set; }
    public string AcademicYear { get; set; } = string.Empty;
    public SemesterType Semester { get; set; }
}

