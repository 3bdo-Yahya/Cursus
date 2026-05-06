namespace Cursus.PL.Models;

public class AdminDashboardViewModel
{
    public int TotalUniversities { get; set; }

    public int TotalGraduationRequirements { get; set; }

    public int TotalDepartments { get; set; }

    public int ActiveDepartments { get; set; }

    public int InactiveDepartments { get; set; }

    public int TotalCourses { get; set; }

    public int ActiveCourses { get; set; }

    public int InactiveCourses { get; set; }
}