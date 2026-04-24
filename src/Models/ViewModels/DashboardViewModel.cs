namespace Cursus.Models.ViewModels
{
    /// <summary>
    /// Represents the data displayed on the student dashboard.
    /// </summary>
    public class DashboardViewModel
    {
        /// <summary>
        /// Gets or sets the student's full name.
        /// </summary>
        public required string StudentName { get; set; }

        /// <summary>
        /// Gets or sets the student's department.
        /// </summary>
        public required string Department { get; set; }

        /// <summary>
        /// Gets or sets the student's cumulative GPA.
        /// </summary>
        public double CGPA { get; set; }

        /// <summary>
        /// Gets or sets the number of completed credit hours.
        /// </summary>
        public int CreditsCompleted { get; set; }

        /// <summary>
        /// Gets or sets the total required credit hours.
        /// </summary>
        public int TotalCreditsRequired { get; set; }

        /// <summary>
        /// Gets or sets the number of remaining courses.
        /// </summary>
        public int CoursesRemaining { get; set; }

        /// <summary>
        /// Gets or sets the academic standing of the student.
        /// </summary>
        public AcademicStanding AcademicStanding { get; set; }

        /// <summary>
        /// Gets or sets the projected graduation date.
        /// </summary>
        public DateOnly? ProjectedGraduationDate { get; set; }

        public string ProjectedGraduationDisplay =>
            ProjectedGraduationDate?.ToString("yyyy") ?? "TBD";

    }
}
