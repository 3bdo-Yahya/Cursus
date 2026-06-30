namespace Cursus.Domain.DTOs;

public record StudentPortalSnapshot(
    StudentDisplayContextDto Display,
    StudentGpaStatsDto Gpa,
    StudentCreditStatsDto Credits,
    StudentGraduationEstimateDto Graduation,
    IReadOnlyList<StudentCurrentCourseDto> CurrentCourses,
    IReadOnlyList<ProgressCategoryDto> ProgressCategories,
    IReadOnlyList<SimulatorCourseDto> SimulatorCurrentCourses,
    IReadOnlyList<ImprovableCourseDto> ImprovableCourses,
    IReadOnlyList<CourseMapNodeDto> CourseMapNodes,
    StudentJsContextDto JsContext);

public record StudentDisplayContextDto(
    string DisplayName,
    string Initials,
    string Department,
    int YearLevel,
    string SemesterLabel,
    string StandingLabel,
    string Subtitle);

public record StudentGpaStatsDto(
    double Cgpa,
    double LastSemesterGpa,
    double CgpaChange,
    double MinGpaForGraduation,
    bool IsOverloadEligible,
    double CompletedQualityPoints);

public record StudentCreditStatsDto(
    int Earned,
    int Required,
    int Remaining,
    int CoursesRemaining,
    int CoreCoursesRemaining,
    int ElectiveCoursesRemaining);

public record StudentGraduationEstimateDto(
    string GraduationSemester,
    string OverloadGraduationSemester,
    int SemestersCompleted,
    int TotalSemesters);

public record StudentCurrentCourseDto(
    string Code,
    string Name,
    string Schedule,
    int CreditHours,
    bool IsElective);

public record ProgressCategoryDto(
    string Name,
    string Subtitle,
    string IconStyle,
    string BarClass,
    string BadgeClass,
    int RequiredCredits,
    int EarnedCredits,
    double Percentage,
    IReadOnlyList<ProgressCourseDto> Courses);

public record ProgressCourseDto(
    string Code,
    string Name,
    int CreditHours,
    string? Grade,
    string Status,
    bool IsLocked);

public record SimulatorCourseDto(
    string Id,
    string Name,
    int Credits);

public record ImprovableCourseDto(
    string Id,
    string Name,
    int Credits,
    string OriginalGrade,
    double OriginalPoints);

public record CourseMapNodeDto(
    string Id,
    string Name,
    int Credits,
    string Type,
    string Avail,
    string Dept,
    string Passing,
    string Status,
    string? Grade,
    IReadOnlyList<string> Prereqs,
    int Year);

public record StudentJsContextDto(
    string Name,
    string Department,
    int Year,
    string Semester,
    double Cgpa,
    string Standing,
    int Completed,
    int Total,
    string Graduation,
    string CompletedCourses,
    string InProgress,
    string Failed);
