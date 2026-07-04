namespace Cursus.PL.Models;

public class StudentDashboardViewModel
{
    public required string StudentName { get; init; }
    public required string Initials { get; init; }
    public required string Department { get; init; }
    public int Year { get; init; }
    public required string Semester { get; init; }
    public required string AcademicStanding { get; init; }
    public required string StandingCssClass { get; init; }
    public required string StandingAlertMessage { get; init; }
    public bool ShowStandingAlert { get; init; }
    public double Cgpa { get; init; }
    public double MaxGpa { get; init; } = 4.0;
    public double CgpaChange { get; init; }
    public int CgpaPercentage => (int)Math.Round(Cgpa / MaxGpa * 100);

    public int CreditsEarned { get; init; }
    public int CreditsRequired { get; init; }
    public double CreditPercentage => CreditsRequired > 0
        ? Math.Round((double)CreditsEarned / CreditsRequired * 100, 1)
        : 0;

    public int CoursesRemaining { get; init; }
    public int CoreCoursesRemaining { get; init; }
    public int ElectiveCoursesRemaining { get; init; }
    public int UniversityRequiredCoursesRemaining { get; init; }

    public required string GraduationSemester { get; init; }
    public int SemestersCompleted { get; init; }
    public int TotalSemesters { get; init; }

    public List<EnrolledCourseViewModel> CurrentCourses { get; init; } = [];
    public int TotalCurrentCredits => CurrentCourses.Sum(c => c.CreditHours);

    public required string UniversityName { get; init; }
    public required string EnrollmentDate { get; init; }
    public double HighestSgpa { get; init; }
    public List<GpaHistoryPointViewModel> GpaHistory { get; init; } = [];
}

public class GpaHistoryPointViewModel
{
    public required string SemLabel { get; init; }
    public double Sgpa { get; init; }
}

public class EnrolledCourseViewModel
{
    public required string Code { get; init; }
    public required string Name { get; init; }
    public required string Schedule { get; init; }
    public int CreditHours { get; init; }
    public bool IsElective { get; init; }
}
