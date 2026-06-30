using Cursus.Domain.DTOs;

namespace Cursus.PL.Models;

public class StudentProgressViewModel
{
    public required string Subtitle { get; init; }
    public required string StandingLabel { get; init; }
    public bool IsOverloadEligible { get; init; }

    public int CreditsEarned { get; init; }
    public int CreditsRequired { get; init; }
    public int CreditsRemaining { get; init; }
    public double CreditPercentage { get; init; }

    public required string GraduationSemester { get; init; }
    public required string OverloadGraduationSemester { get; init; }
    public double MinGpaForGraduation { get; init; }
    public double Cgpa { get; init; }
    public bool MeetsMinGpa { get; init; }

    public int SemestersCompleted { get; init; }
    public int TotalSemesters { get; init; }

    public List<ProgressCategoryViewModel> Categories { get; init; } = [];
}

public class ProgressCategoryViewModel
{
    public required string Name { get; init; }
    public required string Subtitle { get; init; }
    public required string IconStyle { get; init; }
    public required string IconColor { get; init; }
    public required string BarClass { get; init; }
    public required string BadgeClass { get; init; }
    public int RequiredCredits { get; init; }
    public int EarnedCredits { get; init; }
    public double Percentage { get; init; }
    public List<ProgressCourseViewModel> Courses { get; init; } = [];
    public int HiddenCourseCount { get; init; }
}

public class ProgressCourseViewModel
{
    public required string Code { get; init; }
    public required string Name { get; init; }
    public int CreditHours { get; init; }
    public string? Grade { get; init; }
    public required string Status { get; init; }
    public bool IsHiddenByDefault { get; init; }
    public string? GradeClass { get; init; }
}

public class StudentGpaSimulatorViewModel
{
    public required string Subtitle { get; init; }
    public required string StandingLabel { get; init; }
    public double CurrentCgpa { get; init; }
    public double LastSemesterGpa { get; init; }
    public double MinGpaForGraduation { get; init; }
    public int CompletedCredits { get; init; }
    public double CompletedQualityPoints { get; init; }
    public List<SimulatorCourseViewModel> CurrentCourses { get; init; } = [];
    public List<ImprovableCourseViewModel> ImprovableCourses { get; init; } = [];
}

public class SimulatorCourseViewModel
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public int Credits { get; init; }
}

public class ImprovableCourseViewModel
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public int Credits { get; init; }
    public required string OriginalGrade { get; init; }
    public double OriginalPoints { get; init; }
}

public class StudentPageContextViewModel
{
    public required string Subtitle { get; init; }
    public required string DisplayName { get; init; }
    public required string Department { get; init; }
    public int YearLevel { get; init; }
    public required string SemesterLabel { get; init; }
    public required string StandingLabel { get; init; }
    public double Cgpa { get; init; }
    public bool IsOverloadEligible { get; init; }
    public StudentJsContextDto JsContext { get; init; } = null!;
    public IReadOnlyList<CourseMapNodeDto> CourseMapNodes { get; init; } = [];
}

public static class StudentPortalViewModelMapper
{
    public static StudentDashboardViewModel ToDashboard(StudentPortalSnapshot snapshot) =>
        new()
        {
            StudentName = snapshot.Display.DisplayName,
            Initials = snapshot.Display.Initials,
            Department = snapshot.Display.Department,
            Year = snapshot.Display.YearLevel,
            Semester = snapshot.Display.SemesterLabel,
            AcademicStanding = snapshot.Display.StandingLabel,
            Cgpa = snapshot.Gpa.Cgpa,
            CgpaChange = snapshot.Gpa.CgpaChange,
            CreditsEarned = snapshot.Credits.Earned,
            CreditsRequired = snapshot.Credits.Required,
            CoursesRemaining = snapshot.Credits.CoursesRemaining,
            CoreCoursesRemaining = snapshot.Credits.CoreCoursesRemaining,
            ElectiveCoursesRemaining = snapshot.Credits.ElectiveCoursesRemaining,
            GraduationSemester = snapshot.Graduation.GraduationSemester,
            SemestersCompleted = snapshot.Graduation.SemestersCompleted,
            TotalSemesters = snapshot.Graduation.TotalSemesters,
            CurrentCourses = snapshot.CurrentCourses
                .Select(c => new EnrolledCourseViewModel
                {
                    Code = c.Code,
                    Name = c.Name,
                    Schedule = c.Schedule,
                    CreditHours = c.CreditHours,
                    IsElective = c.IsElective
                })
                .ToList(),
            IsOverloadEligible = snapshot.Gpa.IsOverloadEligible
        };

    public static StudentProgressViewModel ToProgress(StudentPortalSnapshot snapshot) =>
        new()
        {
            Subtitle = snapshot.Display.Subtitle,
            StandingLabel = snapshot.Display.StandingLabel,
            IsOverloadEligible = snapshot.Gpa.IsOverloadEligible,
            CreditsEarned = snapshot.Credits.Earned,
            CreditsRequired = snapshot.Credits.Required,
            CreditsRemaining = snapshot.Credits.Remaining,
            CreditPercentage = snapshot.Credits.Required > 0
                ? Math.Round((double)snapshot.Credits.Earned / snapshot.Credits.Required * 100, 1)
                : 0,
            GraduationSemester = snapshot.Graduation.GraduationSemester,
            OverloadGraduationSemester = snapshot.Graduation.OverloadGraduationSemester,
            MinGpaForGraduation = snapshot.Gpa.MinGpaForGraduation,
            Cgpa = snapshot.Gpa.Cgpa,
            MeetsMinGpa = snapshot.Gpa.Cgpa >= snapshot.Gpa.MinGpaForGraduation,
            SemestersCompleted = snapshot.Graduation.SemestersCompleted,
            TotalSemesters = snapshot.Graduation.TotalSemesters,
            Categories = snapshot.ProgressCategories
                .Select(MapCategory)
                .ToList()
        };

    public static StudentGpaSimulatorViewModel ToGpaSimulator(StudentPortalSnapshot snapshot) =>
        new()
        {
            Subtitle = snapshot.Display.Subtitle,
            StandingLabel = snapshot.Display.StandingLabel,
            CurrentCgpa = snapshot.Gpa.Cgpa,
            LastSemesterGpa = snapshot.Gpa.LastSemesterGpa,
            MinGpaForGraduation = snapshot.Gpa.MinGpaForGraduation,
            CompletedCredits = snapshot.Credits.Earned,
            CompletedQualityPoints = snapshot.Gpa.CompletedQualityPoints,
            CurrentCourses = snapshot.SimulatorCurrentCourses
                .Select(c => new SimulatorCourseViewModel { Id = c.Id, Name = c.Name, Credits = c.Credits })
                .ToList(),
            ImprovableCourses = snapshot.ImprovableCourses
                .Select(c => new ImprovableCourseViewModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    Credits = c.Credits,
                    OriginalGrade = c.OriginalGrade,
                    OriginalPoints = c.OriginalPoints
                })
                .ToList()
        };

    public static StudentPageContextViewModel ToPageContext(
        StudentPortalSnapshot snapshot,
        bool includeCourseMap = false) =>
        new()
        {
            Subtitle = snapshot.Display.Subtitle,
            DisplayName = snapshot.Display.DisplayName,
            Department = snapshot.Display.Department,
            YearLevel = snapshot.Display.YearLevel,
            SemesterLabel = snapshot.Display.SemesterLabel,
            StandingLabel = snapshot.Display.StandingLabel,
            Cgpa = snapshot.Gpa.Cgpa,
            IsOverloadEligible = snapshot.Gpa.IsOverloadEligible,
            JsContext = snapshot.JsContext,
            CourseMapNodes = includeCourseMap ? snapshot.CourseMapNodes : []
        };

    private static ProgressCategoryViewModel MapCategory(ProgressCategoryDto category)
    {
        const int visibleCount = 3;
        var courses = category.Courses
            .Select((course, index) => new ProgressCourseViewModel
            {
                Code = course.Code,
                Name = course.Name,
                CreditHours = course.CreditHours,
                Grade = course.Grade,
                Status = course.Status,
                IsHiddenByDefault = index >= visibleCount,
                GradeClass = MapGradeClass(course.Grade)
            })
            .ToList();

        return new ProgressCategoryViewModel
        {
            Name = category.Name,
            Subtitle = category.Subtitle,
            IconStyle = category.IconStyle,
            IconColor = category.BarClass switch
            {
                "cat-bar-purple" => "#7c3aed",
                "cat-bar-amber" => "#b45309",
                "cat-bar-green" => "#047857",
                _ => "var(--c-primary)"
            },
            BarClass = category.BarClass,
            BadgeClass = category.BadgeClass,
            RequiredCredits = category.RequiredCredits,
            EarnedCredits = category.EarnedCredits,
            Percentage = category.Percentage,
            Courses = courses,
            HiddenCourseCount = Math.Max(0, courses.Count - visibleCount)
        };
    }

    private static string? MapGradeClass(string? grade)
    {
        if (string.IsNullOrWhiteSpace(grade))
        {
            return null;
        }

        return grade is "A+" or "A" or "A-" or "B+" or "B" or "B-" ? "grade-a"
            : grade is "C+" or "C" or "C-" or "D+" or "D" ? "grade-b"
            : "grade-f";
    }
}
