using Cursus.Domain.Enums;

namespace Cursus.Domain.DTOs
{
    public record StudentDashboardDto
    {
        public string StudentId      { get; init; } = string.Empty;
        public string DisplayName    { get; init; } = string.Empty;
        public string DepartmentName { get; init; } = "N/A";
        public string AcademicYear   { get; init; } = string.Empty;
        public SemesterType     CurrentSemester { get; init; }
        public AcademicStanding Standing        { get; init; }
        public decimal Cgpa { get; init; }
        public decimal CgpaChange { get; init; }
        public int CreditsCompleted { get; init; }
        public int CreditsRequired { get; init; }
        public int CoursesRemaining         { get; init; }
        public int CoreCoursesRemaining     { get; init; }
        public int ElectiveCoursesRemaining  { get; init; }
        public int UniReqCoursesRemaining    { get; init; }
        public string ProjectedGraduation { get; init; } = "N/A";
        public int SemestersCompleted { get; init; }
        public int TotalSemesters { get; init; }
        public IReadOnlyList<EnrolledCourseDto> CurrentCourses { get; init; } = [];
    }

    public record EnrolledCourseDto(
        string Code,
        string Name,
        int    CreditHours,
        bool   IsElective
    );
}
